using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Wakatime.VSPackageWakaTime
{
    /// <summary>
    /// Singleton class to check plugin .
    /// </summary>
    class WakatimeUtilityManager
    {
        static bool is64BitProcess = (IntPtr.Size == 8);
        static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        private const string PythonUtilityName = "Python.exe";
        private const string WakatimeUtilityName = "wakatime-cli.py";
        private const string VSWakatimePluginName = "VSWakaTimePlugin V1.0";
        
        private Process _process = new Process();
        private string _apiKey = null;

        private static WakatimeUtilityManager _instance;

        private WakatimeUtilityManager() { }

        public string ApiKey
        {
            get
            {
                return _apiKey;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    _apiKey = value;
                    WakatimeConfigFileHelper.updateApiKey(value);
                }
            }
        }

        public static WakatimeUtilityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WakatimeUtilityManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Check Python installed or not, APIKEY exists or not, Command line utility installed or not
        /// </summary>
        /// <returns></returns>
        public void initialize()
        {
            try
            {
                //Check if Python installed or not
                string filePath = GetFullPath(PythonUtilityName);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    string pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.msi";
                    if (is64BitOperatingSystem)
                    {
                        pythonDownloadUrl = "https://www.python.org/ftp/python/3.4.2/python-3.4.2.amd64.msi";
                    }

                    VSWakatimeDownloader.downloadPython(pythonDownloadUrl, PythonUtilityName);
                }

                if (isCommandLineUtilityExist() == false)
                {
                    VSWakatimeDownloader.downloadUtility("https://github.com/wakatime//wakatime//archive//master.zip", WakatimeUtilityName);
                }

                _apiKey = WakatimeConfigFileHelper.getApiKey();

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    VSWakatimeLogger.Instance.writeToLog("API Key could not be found.");
                }
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("WakatimeUtilityManager initialize : " + ex.Message);
            }
        }

        public void sendFile(string fileName, string reasonForSending = "")
        {
            try
            {
                fileName = "\"" + fileName + "\"";
                //For debugging purpose
                //string arguments = "/K " + PythonUtilityName + " " + WakatimeUtilityName + " --key=" + _apiKey + " --file=" + fileName;
                string arguments = WakatimeUtilityName + " --key=" + _apiKey + " --file=" + fileName + " --plugin=" + VSWakatimePluginName;
                if (!string.IsNullOrEmpty(reasonForSending))
                    arguments = arguments + reasonForSending;

                ProcessStartInfo procInfo = new ProcessStartInfo();
                procInfo.UseShellExecute = false;
                procInfo.FileName = PythonUtilityName;
                procInfo.CreateNoWindow = true;
                procInfo.Arguments = arguments;
                procInfo.WorkingDirectory = getCurrentDirectory();

                var proc = Process.Start(procInfo);
            }
            catch (InvalidOperationException ex)
            {
                VSWakatimeLogger.Instance.writeToLog("WakatimeUtilityManager sendFile : " + ex.Message);
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("WakatimeUtilityManager sendFile : " + ex.Message);
            }
        }

        /// <summary>
        /// Is it 64 bit Windows?
        /// http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
        /// </summary>
        /// <returns></returns>
        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if wakatime command line exists or not
        /// </summary>
        /// <returns></returns>
        private bool isCommandLineUtilityExist()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = System.IO.Path.GetDirectoryName(assembly);

            string utilityAbsolutePath = path + "\\wakatime-cli.py";
            if (File.Exists(utilityAbsolutePath))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Given file name it returns absolute path
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            foreach (var path in values.Split(';'))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        /// <summary>
        /// Returns current working dir
        /// </summary>
        /// <returns></returns>
        static public string getCurrentDirectory()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }
    }
}
