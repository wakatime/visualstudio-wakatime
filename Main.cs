using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace WakaTime {

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidWakaTimePkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]

    public sealed class Main : Package
    {

        #region " Fields "
        public const string VERSION = "2.0.3";
        public static string PLUGIN_NAME = "visualstudio-wakatime";
        public static string EDITOR_NAME = "visualstudio";
        public static string EDITOR_VERSION = "";
        public static string CURRENT_PYTHON_VERSION = "3.4.3";
        
        private const int heartbeatInterval = 2; // minutes
        private static EnvDTE.DTE _objDTE = null;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        public static string apiKey = null;
        public static string lastFile = null;
        public static DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        public static Object threadLock = new Object();
        static bool is64BitProcess = (IntPtr.Size == 8);
        static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );
        #endregion

        #region " StartUp/CleanUp "

        public Main()
        {
        }

        protected override void Initialize() {
            IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            Logger.Instance.initialize(log);
            try {
                base.Initialize();

                _objDTE = (DTE)GetService(typeof(DTE));
                _docEvents = _objDTE.Events.DocumentEvents;
                _windowEvents = _objDTE.Events.WindowEvents;
                EDITOR_VERSION = _objDTE.Version;

                // Make sure python is installed
                if (!isPythonInstalled())
                {
                    string url = getPythonDownloadUrl();
                    Downloader.downloadPython(url, getConfigDir());
                }

                if (!doesCLIExist())
                {
                    string url = "https://github.com/wakatime/wakatime/archive/master.zip";
                    Downloader.downloadCLI(url, getConfigDir());
                }

                apiKey = Config.getApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    promptApiKey();
                }

                // Add our command handlers for menu (commands must exist in the .vsct file)
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs) {
                    // Create the command for the menu item.
                    CommandID menuCommandID = new CommandID(GuidList.guidWakaTimeCmdSet, (int)PkgCmdIDList.cmdidUpdateApiKey);
                    MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                    mcs.AddCommand(menuItem);
                }

                // setup event handlers
                _docEvents.DocumentOpened += new _dispDocumentEvents_DocumentOpenedEventHandler(DocumentEvents_DocumentOpened);
                _docEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(DocumentEvents_DocumentSaved);
                _windowEvents.WindowActivated += new _dispWindowEvents_WindowActivatedEventHandler(Window_Activated);

            } catch(Exception ex) {
                Logger.Instance.error(ex.Message);
            }
        }
        #endregion

        #region " Event Handlers "
        public void Window_Activated(Window gotFocus, Window lostFocus) {
            try {
                Document document = _objDTE.ActiveWindow.Document;
                if (document != null) {
                    handleActivity(document.FullName, false);
                }
            } catch(Exception ex) {
                Logger.Instance.error("Window_Activated : " + ex.Message);
            }
        }

        public void DocumentEvents_DocumentOpened(EnvDTE.Document document) {
            try {
                handleActivity(document.FullName, false);
            } catch (Exception ex) {
                Logger.Instance.error("DocumentEvents_DocumentOpened : " + ex.Message);
            }
        }

        public void DocumentEvents_DocumentSaved(EnvDTE.Document document) {
            try {
                handleActivity(document.FullName, true);
            } catch(Exception ex) {
                Logger.Instance.error("DocumentEvents_DocumentSaved : " + ex.Message);
            }
        }
        #endregion

        #region " Methods "

        public static string getPythonDownloadUrl()
        {
            string url = "https://www.python.org/ftp/python/" + CURRENT_PYTHON_VERSION + "/python-" + CURRENT_PYTHON_VERSION;
            if (is64BitOperatingSystem)
            {
                url = url + ".amd64";
            }
            url = url + ".msi";
            return url;
        }

        public static void handleActivity(string currentFile, bool isWrite)
        {
            if (currentFile != null)
            {
                System.Threading.Thread thread = new System.Threading.Thread(
                    delegate()
                    {
                        lock (Main.threadLock)
                        {

                            if (isWrite || Main.lastFile == null || Main.enoughTimePassed() || !currentFile.Equals(Main.lastFile))
                            {
                                sendHeartbeat(currentFile, isWrite);
                                Main.lastFile = currentFile;
                                Main.lastHeartbeat = DateTime.UtcNow;
                            }

                        }
                    }
                );
                thread.Start();
            }
        }

        public static bool enoughTimePassed()
        {
            if (Main.lastHeartbeat == null || Main.lastHeartbeat < DateTime.UtcNow.AddMinutes(-1))
            {
                return true;
            }
            return false;
        }

        public static string getPython() {
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
            foreach (string location in locations) {
                try {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = false;
                    procInfo.RedirectStandardError = true;
                    procInfo.FileName = location;
                    procInfo.CreateNoWindow = true;
                    procInfo.Arguments = "--version";
                    var proc = System.Diagnostics.Process.Start(procInfo);
                    string errors = proc.StandardError.ReadToEnd();
                    if (errors == null || errors == "") {
                        return location;
                    }
                } catch (Exception ex) { }
            }
            return null;
        }

        public static string getConfigDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static string getCLI()
        {
            return getConfigDir() + "\\wakatime-master\\wakatime\\cli.py";
        }

        public static void sendHeartbeat(string fileName, bool isWrite)
        {
            string arguments = "\"" + getCLI() + "\" --key=\"" + apiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"" + EDITOR_NAME + "/" + EDITOR_VERSION + " " + PLUGIN_NAME + "/" + VERSION + "\"";
            
            if (isWrite)
                arguments = arguments + " --write";

            string projectName = getProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
                arguments = arguments + " --project=\"" + projectName + "\"";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = getPython();
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            try {
                var proc = System.Diagnostics.Process.Start(procInfo);
            } catch (InvalidOperationException ex) {
                Logger.Instance.error("Could not send heartbeat : " + getPython() + " " + arguments);
                Logger.Instance.error("Could not send heartbeat : " + ex.Message);
            } catch (Exception ex) {
                Logger.Instance.error("Could not send heartbeat : " + getPython() + " " + arguments);
                Logger.Instance.error("Could not send heartbeat : " + ex.Message);
            }
        }

        public static bool InternalCheckIsWow64() {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6) {
                using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal)) {
                        return false;
                    }
                    return retVal;
                }
            } else {
                return false;
            }
        }

        private static bool doesCLIExist()
        {
            if (File.Exists(getCLI())) {
                return true;
            }
            return false;
        }

        private static bool isPythonInstalled()
        {
            if (getPython() != null) {
                return true;
            }
            return false;
        }

        private void MenuItemCallback(object sender, EventArgs e) {
            try {
                promptApiKey();
            } catch(Exception ex) {
                Logger.Instance.error("MenuItemCallback : " + ex.Message);
            }
        }

        private DialogResult promptApiKey()
        {
            ApiKeyForm form = new ApiKeyForm();
            DialogResult result = form.ShowDialog();
            return result;
        }

        private static string getProjectName() {
            string projectName = _objDTE.Solution != null && !string.IsNullOrWhiteSpace(_objDTE.Solution.FullName) ? _objDTE.Solution.FullName : null;
            if (!string.IsNullOrWhiteSpace(projectName)) {
                return Path.GetFileNameWithoutExtension(projectName);
            }
            return null;
        }

        #endregion

    }
}
