using System;
using System.Linq;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using WakaTime.Forms;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using WakaTime.Shared.ExtensionUtils;
using WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers;
using Configuration = WakaTime.Shared.ExtensionUtils.Configuration;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;
using PackageAutoLoadFlags = WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers.PackageAutoLoadFlags;

namespace WakaTime
{
    [Guid(GuidList.GuidWakaTimePkgString)]
    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Shared.ExtensionUtils.AsyncPackageHelpers.ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F", PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WakaTimePackage : Package, IAsyncLoadablePackageInitialize
    {
        #region Fields
        private static SettingsForm _settingsForm;

        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;
        private DebuggerEvents _debuggerEvents;
        private BuildEvents _buildEvents;
        private TextEditorEvents _textEditorEvents;

        private bool _isBuildRunning;
        private string _runningBuildOutput;

        public static DTE ObjDte;

        private static string _solutionName = string.Empty;

        internal Shared.ExtensionUtils.WakaTime WakaTime;
        #endregion

        #region Startup/Cleanup        
        protected override void Initialize()
        {
            base.Initialize();
            ObjDte = (DTE)GetService(typeof(DTE));

            var configuration = new Configuration
            {
                EditorName = "visualstudio",
                PluginName = "visualstudio-wakatime",
                EditorVersion = ObjDte == null ? string.Empty : ObjDte.Version
            };
            WakaTime = new Shared.ExtensionUtils.WakaTime(this, configuration, new Logger());

            // Only perform initialization if async package framework not supported
            if (WakaTime.IsAsyncLoadSupported) return;

            // Try force initializing in background
            WakaTime.Logger.Debug("Initializing in background thread.");
            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        public IVsTask Initialize(IAsyncServiceProvider pServiceProvider, IProfferAsyncService pProfferService,
            IAsyncProgressCallback pProgressCallback)
        {
            if (!WakaTime.IsAsyncLoadSupported)
                throw new InvalidOperationException(
                    "Async Initialize method should not be called when async load is not supported.");

            return ThreadHelper.JoinableTaskFactory.RunAsync<object>(async () =>
            {
                WakaTime.Logger.Debug("Initializing async.");
                InitializeAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OnStartupComplete();

                return null;
            }).AsVsTask();
        }

        private void InitializeAsync()
        {
            try
            {
                // VisualStudio Object                
                _docEvents = ObjDte.Events.DocumentEvents;
                _windowEvents = ObjDte.Events.WindowEvents;
                _solutionEvents = ObjDte.Events.SolutionEvents;
                _debuggerEvents = ObjDte.Events.DebuggerEvents;
                _buildEvents = ObjDte.Events.BuildEvents;
                _textEditorEvents = ObjDte.Events.TextEditorEvents;

                // Settings Form
                _settingsForm = new SettingsForm(ref WakaTime);
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;

                // Add our command handlers for menu (commands must exist in the .vsct file)
                if (GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.UpdateWakaTimeSettings);
                    var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                    mcs.AddCommand(menuItem);
                }

                // setup event handlers
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _windowEvents.WindowActivated += WindowEventsOnWindowActivated;
                _solutionEvents.Opened += SolutionEventsOnOpened;
                _debuggerEvents.OnEnterRunMode += DebuggerEventsOnEnterRunMode;
                _debuggerEvents.OnEnterDesignMode += DebuggerEventsOnEnterDesignMode;
                _debuggerEvents.OnEnterBreakMode += DebuggerEventsOnEnterBreakMode;
                _buildEvents.OnBuildProjConfigBegin += BuildEventsOnBuildProjConfigBegin;
                _buildEvents.OnBuildProjConfigDone += BuildEventsOnBuildProjConfigDone;
                _textEditorEvents.LineChanged += TextEditorEventsLineChanged;

                WakaTime.InitializeAsync();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("Error Initializing WakaTime", ex);
            }
        }
        #endregion

        #region Event Handlers

        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                WakaTime.HandleActivity(document.FullName, false, GetProjectName(), category);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                WakaTime.HandleActivity(document.FullName, true, GetProjectName(), category);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = ObjDte.ActiveWindow.Document;
                if (document != null)
                {
                    var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                    WakaTime.HandleActivity(document.FullName, false, GetProjectName(), category);
                }
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = ObjDte.Solution.FullName;
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("SolutionEventsOnOpened", ex);
            }
        }

        private void DebuggerEventsOnEnterRunMode(dbgEventReason reason)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                WakaTime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterRunMode", ex);
            }
        }

        private void DebuggerEventsOnEnterDesignMode(dbgEventReason reason)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                WakaTime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterDesignMode", ex);
            }
        }

        private void DebuggerEventsOnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction executionAction)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                WakaTime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("DebuggerEventsOnEnterBreakMode", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigBegin(string project, string projectConfig, string platform, string solutionConfig)
        {
            try
            {
                _isBuildRunning = true;

                var outputFile = GetProjectOutputForConfiguration(project, platform, projectConfig);

                _runningBuildOutput = outputFile;

                WakaTime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Building);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("BuildEventsOnBuildProjConfigBegin", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            try
            {
                _isBuildRunning = false;

                var outputFile = GetProjectOutputForConfiguration(project, platform, projectConfig);

                WakaTime.HandleActivity(outputFile, success, GetProjectName(), HeartbeatCategory.Building);
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("BuildEventsOnBuildProjConfigDone", ex);
            }
        }

        private void TextEditorEventsLineChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            try
            {
                var document = startPoint.Parent.Parent;

                if (document != null)
                {
                    var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : ObjDte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                    WakaTime.HandleActivity(document.FullName, false, GetProjectName(), category);
                }
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("TextEditorEventsLineChanged", ex);
            }
        }        

        private void OnStartupComplete()
        {
            // Prompt for api key if not already set
            if (string.IsNullOrEmpty(WakaTime.Config.ApiKey))
                PromptApiKey();
        }
        #endregion

        #region Methods

        private void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            WakaTime.Config.Read();
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                SettingsPopup();
            }
            catch (Exception ex)
            {
                WakaTime.Logger.Error("MenuItemCallback", ex);
            }
        }

        private void PromptApiKey()
        {
            WakaTime.Logger.Info("Please input your api key into the wakatime window.");
            var form = new ApiKeyForm(ref WakaTime);
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : ObjDte.Solution != null && !string.IsNullOrEmpty(ObjDte.Solution.FullName)
                    ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName)
                    : string.Empty;
        }

        private static string GetProjectOutputForConfiguration(string projectName, string platform, string projectConfig)
        {
            try
            {
                var project = ObjDte.Solution.Projects.Cast<Project>()
                                .FirstOrDefault(proj => proj.UniqueName == projectName);

                var config = project.ConfigurationManager.Cast<EnvDTE.Configuration>()
                                .FirstOrDefault(conf => conf.PlatformName == platform && conf.ConfigurationName == projectConfig);

                var outputPath = config.Properties.Item("OutputPath");
                var outputFileName = project.Properties.Item("OutputFileName");
                var projectPath = project.Properties.Item("FullPath");

                return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
            }
            catch(Exception)
            {
                return null;
            }
        }

        private static string GetCurrentProjectOutputForCurrentConfiguration()
        {
            try
            {
                var activeProjects = (object[])ObjDte.ActiveSolutionProjects;
                if (ObjDte.Solution == null || activeProjects.Length < 1)
                    return null;

                var project = (Project)((object[])ObjDte.ActiveSolutionProjects)[0];
                var config = project.ConfigurationManager.ActiveConfiguration;
                var outputPath = config.Properties.Item("OutputPath");
                var outputFileName = project.Properties.Item("OutputFileName");
                var projectPath = project.Properties.Item("FullPath");

                return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
            }
            catch(Exception)
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _docEvents.DocumentOpened -= DocEventsOnDocumentOpened;
            _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
            _windowEvents.WindowActivated -= WindowEventsOnWindowActivated;
            _solutionEvents.Opened -= SolutionEventsOnOpened;
            _debuggerEvents.OnEnterRunMode -= DebuggerEventsOnEnterRunMode;
            _debuggerEvents.OnEnterDesignMode -= DebuggerEventsOnEnterDesignMode;
            _debuggerEvents.OnEnterBreakMode -= DebuggerEventsOnEnterBreakMode;
            _buildEvents.OnBuildProjConfigBegin -= BuildEventsOnBuildProjConfigBegin;
            _buildEvents.OnBuildProjConfigDone -= BuildEventsOnBuildProjConfigDone;
            _textEditorEvents.LineChanged -= TextEditorEventsLineChanged;

            WakaTime.Dispose();
        }
        #endregion        
    }
}
