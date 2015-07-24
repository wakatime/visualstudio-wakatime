using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using WakaTime.Forms;
using Thread = System.Threading.Thread;

namespace WakaTime
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.GuidWakaTimePkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    public sealed class WakaTimePackage : Package
    {
        #region Fields
        private static string _version = string.Empty;
        private static string _editorVersion = string.Empty;
        private static WakaTimeConfigFile _wakaTimeConfigFile;
        private static SettingsForm _settingsForm;

        private static DTE2 _objDte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;

        public static bool Debug;
        public static string ApiKey;
        private static string _lastFile;
        private static string _solutionName = string.Empty;
        DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();
        #endregion

        #region Startup/Cleanup
        protected override void Initialize()
        {
            _version = string.Format("{0}.{1}.{2}", CoreAssembly.Version.Major, CoreAssembly.Version.Minor, CoreAssembly.Version.Build);

            try
            {
                Logger.Debug(string.Format("Initializing WakaTime v{0}", _version));

                base.Initialize();

                _objDte = (DTE2)GetService(typeof(DTE));
                _docEvents = _objDte.Events.DocumentEvents;
                _windowEvents = _objDte.Events.WindowEvents;
                _solutionEvents = _objDte.Events.SolutionEvents;
                _editorVersion = _objDte.Version;
                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;
                _wakaTimeConfigFile = new WakaTimeConfigFile();

                // Make sure python is installed
                if (!PythonManager.IsPythonInstalled())
                {
                    var url = PythonManager.GetPythonDownloadUrl();
                    Downloader.DownloadPython(url, ConfigDir);
                }

                if (!DoesCliExist() || !IsCliLatestVersion())
                {
                    try
                    {
                        Directory.Delete(ConfigDir + "\\wakatime-master", true);
                    }
                    catch { /* ignored */ }

                    Downloader.DownloadCli(WakaTimeConstants.CliUrl, ConfigDir);
                }

                GetSettings();

                if (string.IsNullOrEmpty(ApiKey))
                    PromptApiKey();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (mcs != null)
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

                Logger.Info(string.Format("Finished initializing WakaTime v{0}", _version));
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing Wakatime", ex);
            }
        }

        #endregion

        #region Event Handlers
        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                HandleActivity(document.FullName, true);
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = _objDte.ActiveWindow.Document;
                if (document != null)
                    HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = _objDte.Solution.FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("SolutionEventsOnOpened", ex);
            }
        }
        #endregion

        #region Methods

        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            _wakaTimeConfigFile.Read();
            GetSettings();
        }

        private static void GetSettings()
        {
            ApiKey = _wakaTimeConfigFile.ApiKey;
            Debug = _wakaTimeConfigFile.Debug;
        }

        private void HandleActivity(string currentFile, bool isWrite)
        {
            if (currentFile == null) return;

            var thread = new Thread(
                delegate()
                {
                    lock (ThreadLock)
                    {
                        if (!isWrite && _lastFile != null && !EnoughTimePassed() && currentFile.Equals(_lastFile))
                            return;

                        SendHeartbeat(currentFile, isWrite);
                        _lastFile = currentFile;
                        _lastHeartbeat = DateTime.UtcNow;
                    }
                });
            thread.Start();
        }

        private bool EnoughTimePassed()
        {
            return _lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        static string ConfigDir
        {
            get { return Application.UserAppDataPath; }
        }

        static string GetCli()
        {
            return Path.Combine(ConfigDir, WakaTimeConstants.CliFolder);
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            var arguments = new List<string>
            {
                GetCli(),
                "--key",
                ApiKey,
                "--file",
                fileName,
                "--plugin",
                WakaTimeConstants.EditorName + "/" + _editorVersion + " " + WakaTimeConstants.PluginName + "/" + _version
            };

            if (isWrite)
                arguments.Add("--write");

            var projectName = GetProjectName();
            if (!string.IsNullOrEmpty(projectName))
            {
                arguments.Add("--project");
                arguments.Add(projectName);
            }

            var pythonBinary = PythonManager.GetPython();
            if (pythonBinary != null)
            {
                var process = new RunProcess(pythonBinary, arguments.ToArray());
                if (Debug)
                {
                    Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", arguments)));
                    process.Run();
                    Logger.Debug(string.Format("CLI STDOUT: {0}", process.Output));
                    Logger.Debug(string.Format("CLI STDERR: {0}", process.Error));
                }
                else
                    process.RunInBackground();
            }
            else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        static bool DoesCliExist()
        {
            return File.Exists(GetCli());
        }

        static bool IsCliLatestVersion()
        {
            var process = new RunProcess(PythonManager.GetPython(), GetCli(), "--version");
            process.Run();

            return process.Success && process.Error.Equals(WakaTimeConstants.CurrentWakaTimeCliVersion);
        }

        private static void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                SettingsPopup();
            }
            catch (Exception ex)
            {
                Logger.Error("MenuItemCallback", ex);
            }
        }

        private static void PromptApiKey()
        {
            var form = new ApiKeyForm();
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
                : (_objDte.Solution != null && !string.IsNullOrEmpty(_objDte.Solution.FullName))
                    ? Path.GetFileNameWithoutExtension(_objDte.Solution.FullName)
                    : null;
        }
        #endregion

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
