using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
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
        private const string CurrentWakaTimeCLIVersion = "4.0.14"; // https://github.com/wakatime/wakatime/blob/master/HISTORY.rst
        private const string CliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        private static string _version = string.Empty;
        private const string PluginName = "visualstudio-wakatime";
        private const string EditorName = "visualstudio";
        private static string _editorVersion = string.Empty;
        private static string _pythonBinaryLocation = null;
        private const string CurrentPythonVersion = "3.4.3";

        private static DTE _objDte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;

        public static string ApiKey;
        private static string _lastFile;
        private static string _configDir = null;
        DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();
        static readonly bool Is64BitProcess = (IntPtr.Size == 8);
        static readonly bool Is64BitOperatingSystem = Is64BitProcess || InternalCheckIsWow64();
        #endregion

        #region StartUp/CleanUp
        protected override void Initialize()
        {
            var log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            Logger.Instance.Initialize(log);
            try
            {
                base.Initialize();

                _objDte = (DTE)GetService(typeof(DTE));
                _docEvents = _objDte.Events.DocumentEvents;
                _windowEvents = _objDte.Events.WindowEvents;
                _version = CoreAssembly.Version.Major.ToString() + '.' + CoreAssembly.Version.Minor.ToString() + '.' + CoreAssembly.Version.Build.ToString();
                _editorVersion = _objDte.Version;                

                // Make sure python is installed
                if (!IsPythonInstalled())
                {
                    var url = GetPythonDownloadUrl();
                    Downloader.DownloadPython(url, GetConfigDir());
                }

                if (!DoesCliExist() || !IsCliLatestVersion())
                {
                    try
                    {
                        Directory.Delete(GetConfigDir() + "\\wakatime-master", true);
                    }
                    catch { /* ignored */ }

                    Downloader.DownloadCli(CliUrl, GetConfigDir());
                }

                ApiKey = Config.GetApiKey();

                if (string.IsNullOrWhiteSpace(ApiKey))                
                    PromptApiKey();                

                // Add our command handlers for menu (commands must exist in the .vsct file)
                var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs)
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

        private static string GetPythonDownloadUrl()
        {
            var url = "https://www.python.org/ftp/python/" + CurrentPythonVersion + "/python-" + CurrentPythonVersion;

            if (Is64BitOperatingSystem)            
                url = url + ".amd64";
            
            url = url + ".msi";

            return url;
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

        public static string GetPython()
        {
            if (_pythonBinaryLocation != null)
                return _pythonBinaryLocation;

            string[] locations = {
                "pythonw",
                "python",
                "\\Python37\\pythonw",
                "\\Python36\\pythonw",
                "\\Python35\\pythonw",
                "\\Python34\\pythonw",
                "\\Python33\\pythonw",
                "\\Python32\\pythonw",
                "\\Python31\\pythonw",
                "\\Python30\\pythonw",
                "\\Python27\\pythonw",
                "\\Python26\\pythonw",
                "\\python37\\pythonw",
                "\\python36\\pythonw",
                "\\python35\\pythonw",
                "\\python34\\pythonw",
                "\\python33\\pythonw",
                "\\python32\\pythonw",
                "\\python31\\pythonw",
                "\\python30\\pythonw",
                "\\python27\\pythonw",
                "\\python26\\pythonw",
                "\\Python37\\python",
                "\\Python36\\python",
                "\\Python35\\python",
                "\\Python34\\python",
                "\\Python33\\python",
                "\\Python32\\python",
                "\\Python31\\python",
                "\\Python30\\python",
                "\\Python27\\python",
                "\\Python26\\python",
                "\\python37\\python",
                "\\python36\\python",
                "\\python35\\python",
                "\\python34\\python",
                "\\python33\\python",
                "\\python32\\python",
                "\\python31\\python",
                "\\python30\\python",
                "\\python27\\python",
                "\\python26\\python",
            };
            foreach (string location in locations)
            {
                RunProcess process = new RunProcess(location, "--version");

                process.Run();

                if (process.OK())
                {
                    _pythonBinaryLocation = location;
                    return location;
                }
            }
            return null;
        }

        public static string GetConfigDir()
        {
            if (_configDir != null)
                return _configDir;
            _configDir = Application.UserAppDataPath;
            return _configDir;
        }

        public static string GetCli()
        {
            return GetConfigDir() + "\\wakatime-master\\wakatime\\cli.py";
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            List<String> arguments = new List<String>();

            arguments.Add(GetCli());
            arguments.Add("--key");
            arguments.Add(ApiKey);
            arguments.Add("--file");
            arguments.Add(fileName);
            arguments.Add("--plugin");
            arguments.Add(EditorName + "/" + _editorVersion + " " + PluginName + "/" + _version);

            if (isWrite)
                arguments.Add("--write");

            var projectName = GetProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                arguments.Add("--project");
                arguments.Add(projectName);
            }

            RunProcess process = new RunProcess(GetPython(), arguments.ToArray());
            process.RunInBackground();
        }

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major != 5 || Environment.OSVersion.Version.Minor < 1) &&
                Environment.OSVersion.Version.Major < 6) return false;

            using (var p = Process.GetCurrentProcess())
            {
                bool retVal;
                return NativeMethods.IsWow64Process(p.Handle, out retVal) && retVal;
            }
        }

        private static bool DoesCliExist()
        {
            return File.Exists(GetCli());
        }

        private static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        private static bool IsCliLatestVersion()
        {
            RunProcess process = new RunProcess(GetPython(), GetCli(), "--version");
            process.Run();
            if (process.OK() && process.Error().Equals(CurrentWakaTimeCLIVersion))
            {
                return true;
            }

            return false;
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
            var projectName = _objDte.Solution != null && !string.IsNullOrWhiteSpace(_objDte.Solution.FullName) ? _objDte.Solution.FullName : null;
            return !string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(projectName) : null;
        }

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
        #endregion
    }
}
