using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WakaTime.WakaTime {
    /// <summary>
    /// Singleton class to check plugin .
    /// </summary>
    class UtilityManager {
        static bool is64BitProcess = (IntPtr.Size == 8);
        static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );
        
        private const string PLUGIN_NAME = "visualstudio-wakatime";
        private const string VERSION = "2.0.2";
        
        private Process _process = new Process();
        private string _apiKey = null;

        private static UtilityManager _instance;

        private UtilityManager() { }
        
        public string ApiKey {
            get {
                return _apiKey;
            }
            set {
                if (string.IsNullOrWhiteSpace(value) == false) {
                    _apiKey = value;
                    ConfigFileHelper.updateApiKey(value);
                }
            }
        }
        
        public static UtilityManager Instance {
            get {
                if (_instance == null) {
                    _instance = new UtilityManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Check Python installed or not, APIKEY exists or not, Command line utility installed or not
        /// </summary>
        /// <returns></returns>
        public void initialize() {
            try {
                // Make sure python is installed
                if (!isPythonInstalled()) {
                    Logger.Instance.info("UtilityManager: Python not found.");
                    string pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
                    if (is64BitOperatingSystem) {
                        pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
                    }
                    Downloader.downloadPython(pythonDownloadUrl, null);
                } else {
                    Logger.Instance.info("UtilityManager: Python found at " + getPython());
                }

                if (!doesCLIExist()) {
                    Logger.Instance.info("UtilityManager: wakatime-cli not found.");
                    Downloader.downloadCLI("https://github.com/wakatime/wakatime/archive/master.zip", getCLIDir());
                } else {
                    Logger.Instance.info("UtilityManager: wakatime-cli found at " + getCLI());
                }

                _apiKey = ConfigFileHelper.getApiKey();

                if (string.IsNullOrWhiteSpace(_apiKey)) {
                    Logger.Instance.error("API Key could not be found.");
                }
            } catch(Exception ex) {
                Logger.Instance.error("UtilityManager initialize : " + ex.Message);
            }
        }
        
        public string getPythonDir() {
            return getCurrentDirectory() + "\\Python";
        }

        public string getPython() {
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
                    var proc = Process.Start(procInfo);
                    string errors = proc.StandardError.ReadToEnd();
                    if (errors == null || errors == "") {
                        return location;
                    }
                } catch (Exception ex) { }
            }
            return null;
        }

        public string getCLIDir() {
            return getCurrentDirectory() + "\\wakatime";
        }
        
        public string getCLI() {
            return getCLIDir() + "\\wakatime-master\\wakatime-cli.py";
        }

        public void sendFile(string fileName, string projectName, bool isWrite, string visualStudioVersion) {
            string arguments = "\"" + getCLI() + "\" --key=\"" + _apiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"visualstudio/" + visualStudioVersion + " " + PLUGIN_NAME + "/" + VERSION + "\"";

            if (!string.IsNullOrWhiteSpace(projectName))
                arguments = arguments + " --project=\"" + projectName + "\"";
            
            if (isWrite)
                arguments = arguments + " --write";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = getPython();
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            try {
                var proc = Process.Start(procInfo);
            } catch (InvalidOperationException ex) {
                Logger.Instance.error("UtilityManager sendFile : " + getPython() + " " + arguments);
                Logger.Instance.error("UtilityManager sendFile : " + ex.Message);
            } catch (Exception ex) {
                Logger.Instance.error("UtilityManager sendFile : " + getPython() + " " + arguments);
                Logger.Instance.error("UtilityManager sendFile : " + ex.Message);
            }
        }

        /// <summary>
        /// Is it 64 bit Windows?
        /// http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
        /// </summary>
        /// <returns></returns>
        public static bool InternalCheckIsWow64() {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6) {
                using (Process p = Process.GetCurrentProcess()) {
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

        /// <summary>
        /// Check if wakatime command line exists or not
        /// </summary>
        /// <returns></returns>
        private bool doesCLIExist() {
            if (File.Exists(getCLI())) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if bundled python installation exists
        /// </summary>
        /// <returns></returns>
        private bool doesPythonExist() {
            if (File.Exists(getPythonDir() + "\\pythonw.exe")) {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Check if python is installed
        /// </summary>
        /// <returns></returns>
        private bool isPythonInstalled() {
            if (getPython() != null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns current working dir
        /// </summary>
        /// <returns></returns>
        static public string getCurrentDirectory() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }
    }
}
