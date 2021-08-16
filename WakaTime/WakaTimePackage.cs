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
    [Guid(GuidList.GuidWakaTimePkgString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideService(typeof(WakaTimePackage), IsAsyncQueryable = true)]
    [ProvideAutoLoad(GuidList.GuidWakaTimeUIString, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WakaTimePackage : AsyncPackage
    {
        private DTE _dte;
        private Shared.ExtensionUtils.WakaTime _wakatime;
        private ILogger _logger;
        private SettingsForm _settingsForm;
        private bool _isBuildRunning;
        private string _solutionName;

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

            var configuration = new Shared.ExtensionUtils.Configuration
            {
                EditorName = "visualstudio",
                PluginName = "visualstudio-wakatime",
                EditorVersion = _dte == null ? string.Empty : _dte.Version,
                PluginVersion = Constants.PluginVersion
            };

            _logger = new Logger();

            _wakatime = new Shared.ExtensionUtils.WakaTime(configuration, _logger);

            _logger.Debug("It will load WakaTime extension");

            await InitializeAsync(cancellationToken);

            // Prompt for api key if not already set
            if (string.IsNullOrEmpty(_wakatime.Config.ApiKey))
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
                _wakatime.Initialize();

                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Visual Studio Events              
                var docEvents = _dte.Events.DocumentEvents;
                var windowEvents = _dte.Events.WindowEvents;
                var solutionEvents = _dte.Events.SolutionEvents;
                var debuggerEvents = _dte.Events.DebuggerEvents;
                var buildEvents = _dte.Events.BuildEvents;
                var textEditorEvents = _dte.Events.TextEditorEvents;

                // Settings Form
                _settingsForm = new SettingsForm(_wakatime.Config, _logger);
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;

                // Add our command handlers for menu (commands must exist in the .vsct file)
                if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(new Guid(GuidList.GuidWakaTimeCmdSetString), 0x100);
                    var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                    mcs.AddCommand(menuItem);
                }

                // setup event handlers
                docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                windowEvents.WindowActivated += WindowEventsOnWindowActivated;
                solutionEvents.Opened += SolutionEventsOnOpened;
                debuggerEvents.OnEnterRunMode += DebuggerEventsOnEnterRunMode;
                debuggerEvents.OnEnterDesignMode += DebuggerEventsOnEnterDesignMode;
                debuggerEvents.OnEnterBreakMode += DebuggerEventsOnEnterBreakMode;
                buildEvents.OnBuildProjConfigBegin += BuildEventsOnBuildProjConfigBegin;
                buildEvents.OnBuildProjConfigDone += BuildEventsOnBuildProjConfigDone;
                textEditorEvents.LineChanged += TextEditorEventsLineChanged;
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

        private void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            _wakatime.Config.Read();
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
