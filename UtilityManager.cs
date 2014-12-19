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
        private const string VERSION = "1.0.2";
        
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
                //Check if Python installed or not
                if (!doesPythonExist()) {
                    string pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
                    if (is64BitOperatingSystem) {
                        pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
                    }
                    Downloader.downloadPython(pythonDownloadUrl, getPythonDir());
                }

                if (doesCLIExist() == false) {
                    Downloader.downloadCLI("https://github.com/wakatime/wakatime/archive/master.zip", getCLIDir());
                }

                _apiKey = ConfigFileHelper.getApiKey();

                if (string.IsNullOrWhiteSpace(_apiKey)) {
                    Logger.Instance.writeToLog("API Key could not be found.");
                }
            }
            catch(Exception ex) {
                Logger.Instance.writeToLog("UtilityManager initialize : " + ex.Message);
            }
        }

        public string getPythonDir() {
            return getCurrentDirectory() + "\\Python";
        }

        public string getCLIDir() {
            return getCurrentDirectory() + "\\wakatime";
        }

        public string getCLI() {
            return getCLIDir() + "\\wakatime-master\\wakatime-cli.py";
        }

        public void sendFile(string fileName, string projectName = "", string reasonForSending = "") {
            try {
                //For debugging purpose
                //string arguments = "/K " + PythonUtilityName + " " + WakatimeUtilityName + " --key=" + _apiKey + " --file=" + fileName;
                string arguments = getCLI() + " --key=\"" + _apiKey + "\""
                                    + " --file=\"" + fileName + "\""
                                    + " --plugin=" + PLUGIN_NAME + "/" + VERSION;

                if (!string.IsNullOrEmpty(projectName))
                    arguments = arguments + " --project=\"" + projectName + "\"";

                if (!string.IsNullOrEmpty(reasonForSending))
                    arguments = arguments + reasonForSending;

                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.UseShellExecute = false;
                procInfo.FileName = "pythonw";
                procInfo.CreateNoWindow = true;
                procInfo.Arguments = arguments;
                procInfo.WorkingDirectory = getPythonDir();

                var proc = Process.Start(procInfo);
            }
            catch (InvalidOperationException ex) {
                Logger.Instance.writeToLog("UtilityManager sendFile : " + ex.Message);
            }
            catch(Exception ex) {
                Logger.Instance.writeToLog("UtilityManager sendFile : " + ex.Message);
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
        /// Check if local python installation exists
        /// </summary>
        /// <returns></returns>
        private bool doesPythonExist() {
            if (File.Exists(getPythonDir() + "\\pythonw.exe")) {
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
