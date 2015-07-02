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
using Microsoft.VisualStudio.Shell.Interop;
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

        private static DTE2 _objDte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;

        public static string ApiKey;
        private static string _lastFile;        
        DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();        
        #endregion

        #region Startup/Cleanup
        protected override void Initialize()
        {
            var log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            Logger.Instance.Initialize(log);
            try
            {
                base.Initialize();

                _objDte = (DTE2)GetService(typeof(DTE));
                _docEvents = _objDte.Events.DocumentEvents;
                _windowEvents = _objDte.Events.WindowEvents;
                _version = string.Format("{0}.{1}.{2}", CoreAssembly.Version.Major, CoreAssembly.Version.Minor, CoreAssembly.Version.Build);
                _editorVersion = _objDte.Version;         
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

                ApiKey = _wakaTimeConfigFile.ApiKey;

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
                _docEvents.DocumentOpened += DocumentEvents_DocumentOpened;
                _docEvents.DocumentSaved += DocumentEvents_DocumentSaved;
                _windowEvents.WindowActivated += Window_Activated;

            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }
        #endregion

        #region Event Handlers
        public void Window_Activated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = _objDte.ActiveWindow.Document;
                if (document != null)
                    HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Window_Activated : " + ex.Message);
            }
        }

        public void DocumentEvents_DocumentOpened(Document document)
        {
            try
            {
                HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("DocumentEvents_DocumentOpened : " + ex.Message);
            }
        }

        public void DocumentEvents_DocumentSaved(Document document)
        {
            try
            {
                HandleActivity(document.FullName, true);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("DocumentEvents_DocumentSaved : " + ex.Message);
            }
        }
        #endregion

        #region Methods

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
            return Path.Combine(ConfigDir, PythonManager.CliPath);            
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

            var process = new RunProcess(PythonManager.GetPython(), arguments.ToArray());
            process.RunInBackground();            
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
                Logger.Instance.Error("MenuItemCallback : " + ex.Message);
            }
        }

        private static void PromptApiKey()
        {
            var form = new ApiKeyForm();
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            var form = new SettingsForm();
            form.ShowDialog();
        }

        private static string GetProjectName()
        {
            var projectName = _objDte.Solution != null && !string.IsNullOrEmpty(_objDte.Solution.FullName) ? _objDte.Solution.FullName : null;
            return !string.IsNullOrEmpty(projectName) ? Path.GetFileNameWithoutExtension(projectName) : null;
        }
        #endregion

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
