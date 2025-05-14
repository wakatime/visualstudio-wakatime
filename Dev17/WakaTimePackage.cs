using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WakaTime.ExtensionUtils;
using WakaTime.Forms;
using WakaTime.Shared.ExtensionUtils;
using Task = System.Threading.Tasks.Task;

namespace WakaTime
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [Guid(GuidList2022.GuidWakaTimePkgString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideService(typeof(WakaTimePackage), IsAsyncQueryable = true)]
    [ProvideAutoLoad(GuidList.GuidWakaTimeUIString, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WakaTimePackage : AsyncPackage
    {
        private DTE _dte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;
        private DebuggerEvents _debuggerEvents;
        private BuildEvents _buildEvents;
        private TextEditorEvents _textEditorEvents;
        private Shared.ExtensionUtils.WakaTime _wakatime;
        private ILogger _logger;
        private SettingsForm _settingsForm;
        private bool _isBuildRunning;
        private string _solutionName;
        private StatusbarControl _statusbarControl;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            var objDte = await GetServiceAsync(typeof(DTE));
            _dte = objDte as DTE;

            var metadata = new Metadata
            {
                EditorName = "visualstudio",
                PluginName = "visualstudio-wakatime",
                EditorVersion = _dte == null ? string.Empty : _dte.Version,
                PluginVersion = Constants.PluginVersion
            };

            _logger = new Logger(Dependencies.GetConfigFilePath());
            _wakatime = new Shared.ExtensionUtils.WakaTime(metadata, _logger);

            _logger.Debug("It will load WakaTime extension");

            await InitializeAsync(cancellationToken);

            // Prompt for api key if not already set
            if (string.IsNullOrEmpty(_wakatime.Config.GetSetting("api_key")))
                PromptApiKey();
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (_dte is null)
            {
                _logger.Error("DTE is null");
                return;
            }

            try
            {
                // Initialize _wakatime in background, which may take several seconds
                Task wakaTimeInitializationTask = _wakatime.InitializeAsync();

                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Initialize status bar if enabled
                if (_wakatime.Config.GetSettingAsBoolean("status_bar_enabled", true))
                {
                    // Inject control to status bar
                    _statusbarControl = new StatusbarControl();
                    _statusbarControl.SetText("Initializing...");
                    _statusbarControl.SetToolTip("WakaTime: Initializing...");
                    await StatusbarInjector.InjectControlAsync(_statusbarControl);
                }

                // Wait for _wakatime to complete initialization，and display today's coding time on status bar
                await wakaTimeInitializationTask;
                if (_statusbarControl != null)
                {
                    UpdateTimeOnStatusbarControl(_wakatime.TotalTimeToday, _wakatime.TotalTimeTodayDetailed);
                    _wakatime.TotalTimeTodayUpdated += WakatimeTotalTimeTodayUpdated;
                }

                // Visual Studio Events              
                _docEvents = _dte.Events.DocumentEvents;
                _windowEvents = _dte.Events.WindowEvents;
                _solutionEvents = _dte.Events.SolutionEvents;
                _debuggerEvents = _dte.Events.DebuggerEvents;
                _buildEvents = _dte.Events.BuildEvents;
                _textEditorEvents = _dte.Events.TextEditorEvents;

                // Settings Form
                _settingsForm = new SettingsForm(_wakatime.Config, _logger);

                // Add our command handlers for menu (commands must exist in the .vsct file)
                if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(new Guid(GuidList.GuidWakaTimeCmdSetString), 0x100);
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
            }
            catch (Exception ex)
            {
                _logger.Error("Error Initializing WakaTime", ex);
            }
        }

        private void PromptApiKey()
        {
            _logger.Debug("It will ask for user to input its api key");

            var form = new ApiKeyForm(_wakatime.Config, _logger);

            form.ShowDialog();
        }

        private string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : _dte.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName)
                    ? Path.GetFileNameWithoutExtension(_dte.Solution.FullName)
                    : string.Empty;
        }

        private string GetCurrentProjectOutputForCurrentConfiguration()
        {
            try
            {
                var activeProjects = (object[])_dte.ActiveSolutionProjects;
                if (_dte.Solution == null || activeProjects.Length < 1)
                    return null;

                var project = (Project)((object[])_dte.ActiveSolutionProjects)[0];
                var config = project.ConfigurationManager.ActiveConfiguration;
                var outputPath = config.Properties.Item("OutputPath");
                var outputFileName = project.Properties.Item("OutputFileName");
                var projectPath = project.Properties.Item("FullPath");

                return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetProjectOutputForConfiguration(string projectName, string platform, string projectConfig)
        {
            try
            {
                var project = _dte.Solution.Projects.Cast<Project>()
                                .FirstOrDefault(proj => proj.UniqueName == projectName);

                var config = project.ConfigurationManager.Cast<EnvDTE.Configuration>()
                                .FirstOrDefault(conf => conf.PlatformName == platform && conf.ConfigurationName == projectConfig);

                var outputPath = config.Properties.Item("OutputPath");
                var outputFileName = project.Properties.Item("OutputFileName");
                var projectPath = project.Properties.Item("FullPath");

                return $"{projectPath.Value}{outputPath.Value}{outputFileName.Value}";
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                _settingsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.Error("MenuItemCallback", ex);
            }
        }

        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : _dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                _wakatime.HandleActivity(document.FullName, false, GetProjectName(), category);
            }
            catch (Exception ex)
            {
                _logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : _dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                _wakatime.HandleActivity(document.FullName, true, GetProjectName(), category);
            }
            catch (Exception ex)
            {
                _logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = _dte.ActiveWindow.Document;
                if (document != null)
                {
                    var category = _isBuildRunning
                        ? HeartbeatCategory.Building
                        : _dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                    _wakatime.HandleActivity(document.FullName, false, GetProjectName(), category);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = _dte.Solution.FullName;
            }
            catch (Exception ex)
            {
                _logger.Error("SolutionEventsOnOpened", ex);
            }
        }

        private void DebuggerEventsOnEnterRunMode(dbgEventReason reason)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                _wakatime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                _logger.Error("DebuggerEventsOnEnterRunMode", ex);
            }
        }

        private void DebuggerEventsOnEnterDesignMode(dbgEventReason reason)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                _wakatime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                _logger.Error("DebuggerEventsOnEnterDesignMode", ex);
            }
        }

        private void DebuggerEventsOnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction executionAction)
        {
            try
            {
                var outputFile = GetCurrentProjectOutputForCurrentConfiguration();

                _wakatime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Debugging);
            }
            catch (Exception ex)
            {
                _logger.Error("DebuggerEventsOnEnterBreakMode", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigBegin(
            string project, string projectConfig, string platform, string solutionConfig)
        {
            try
            {
                _isBuildRunning = true;

                var outputFile = GetProjectOutputForConfiguration(project, platform, projectConfig);

                _wakatime.HandleActivity(outputFile, false, GetProjectName(), HeartbeatCategory.Building);
            }
            catch (Exception ex)
            {
                _logger.Error("BuildEventsOnBuildProjConfigBegin", ex);
            }
        }

        private void BuildEventsOnBuildProjConfigDone(
            string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            try
            {
                _isBuildRunning = false;

                var outputFile = GetProjectOutputForConfiguration(project, platform, projectConfig);

                _wakatime.HandleActivity(outputFile, success, GetProjectName(), HeartbeatCategory.Building);
            }
            catch (Exception ex)
            {
                _logger.Error("BuildEventsOnBuildProjConfigDone", ex);
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
                        : _dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode
                            ? HeartbeatCategory.Debugging
                            : HeartbeatCategory.Coding;

                    _wakatime.HandleActivity(document.FullName, false, GetProjectName(), category);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("TextEditorEventsLineChanged", ex);
            }
        }

        private void WakatimeTotalTimeTodayUpdated(object sender, TotalTimeTodayUpdatedEventArgs e)
        {
            _ = JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                UpdateTimeOnStatusbarControl(e.TotalTimeToday, e.TotalTimeTodayDetailed);
            });
        }

        private void UpdateTimeOnStatusbarControl(string totalTimeToday, string totalTimeTodayDetailed)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string text = string.IsNullOrEmpty(totalTimeToday) ? "WakaTime" : totalTimeToday;
            _statusbarControl?.SetText(text);

            string toolTip = "WakaTime: Today's coding time";
            if (!string.IsNullOrEmpty(totalTimeTodayDetailed))
            {
                toolTip += Environment.NewLine + totalTimeTodayDetailed;
            }
            _statusbarControl?.SetToolTip(toolTip);
        }
    }

    internal static class CoreAssembly
    {
        private static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;
    }

    internal static class Constants
    {
        internal static readonly string PluginVersion =
            $"{CoreAssembly.Version.Major}" +
            $".{CoreAssembly.Version.Minor}" +
            $".{CoreAssembly.Version.Build}";
    }
}
