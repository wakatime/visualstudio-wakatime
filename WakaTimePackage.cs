using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        public const string Version = "4.0.3";
        public static string PluginName = "visualstudio-wakatime";
        public static string EditorName = "visualstudio";
        public static string EditorVersion = "";
        public static string CurrentPythonVersion = "3.4.3";

        private const int HeartbeatInterval = 2; // minutes
        private static DTE _objDte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        public static string ApiKey;
        public static string LastFile;
        public static DateTime LastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        public static Object ThreadLock = new Object();
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
                EditorVersion = _objDte.Version;

                // Make sure python is installed
                if (!IsPythonInstalled())
                {
                    var url = GetPythonDownloadUrl();
                    Downloader.DownloadPython(url, GetConfigDir());
                }

                if (!DoesCliExist())
                {
                    const string url = "https://github.com/wakatime/wakatime/archive/master.zip";
                    Downloader.DownloadCli(url, GetConfigDir());
                }

                ApiKey = Config.GetApiKey();

                if (string.IsNullOrWhiteSpace(ApiKey))                
                    PromptApiKey();                

                // Add our command handlers for menu (commands must exist in the .vsct file)
                var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.CmdidUpdateApiKey);
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
        public static string GetPythonDownloadUrl()
        {
            var url = "https://www.python.org/ftp/python/" + CurrentPythonVersion + "/python-" + CurrentPythonVersion;

            if (Is64BitOperatingSystem)            
                url = url + ".amd64";
            
            url = url + ".msi";

            return url;
        }

        public static void HandleActivity(string currentFile, bool isWrite)
        {
            if (currentFile != null)
            {
                var thread = new Thread(
                    delegate()
                    {
                        lock (ThreadLock)
                        {
                            if (isWrite || LastFile == null || EnoughTimePassed() || !currentFile.Equals(LastFile))
                            {
                                SendHeartbeat(currentFile, isWrite);
                                LastFile = currentFile;
                                LastHeartbeat = DateTime.UtcNow;
                            }
                        }
                    }
                );
                thread.Start();
            }
        }

        public static bool EnoughTimePassed()
        {
            return LastHeartbeat == null || LastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public static string GetPython()
        {
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

            foreach (var location in locations)
            {
                try
                {
                    var procInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        FileName = location,
                        CreateNoWindow = true,
                        Arguments = "--version"
                    };
                    var proc = Process.Start(procInfo);
                    var errors = proc.StandardError.ReadToEnd();
                    if (string.IsNullOrEmpty(errors))                    
                        return location;
                }
                catch { /* ignored */ }
            }
            return null;
        }

        public static string GetConfigDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static string GetCli()
        {
            return GetConfigDir() + "\\wakatime-master\\wakatime\\cli.py";
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            var arguments = "\"" + GetCli() + "\" --key=\"" + ApiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"" + EditorName + "/" + EditorVersion + " " + PluginName + "/" + Version + "\"";

            if (isWrite)
                arguments = arguments + " --write";

            var projectName = GetProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
                arguments = arguments + " --project=\"" + projectName + "\"";

            var procInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = GetPython(),
                CreateNoWindow = true,
                Arguments = arguments
            };

            try
            {
                Process.Start(procInfo);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Instance.Error("Could not send heartbeat : " + GetPython() + " " + arguments);
                Logger.Instance.Error("Could not send heartbeat : " + ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Could not send heartbeat : " + GetPython() + " " + arguments);
                Logger.Instance.Error("Could not send heartbeat : " + ex.Message);
            }            
        }

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
            {
                using (var p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    return NativeMethods.IsWow64Process(p.Handle, out retVal) && retVal;
                }
            }

            return false;
        }

        private static bool DoesCliExist()
        {
            return File.Exists(GetCli());
        }

        private static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                PromptApiKey();
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

        private static string GetProjectName()
        {
            var projectName = _objDte.Solution != null && !string.IsNullOrWhiteSpace(_objDte.Solution.FullName) ? _objDte.Solution.FullName : null;
            return !string.IsNullOrWhiteSpace(projectName) ? Path.GetFileNameWithoutExtension(projectName) : null;
        }
        #endregion
    }
}
