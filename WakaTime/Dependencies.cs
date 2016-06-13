using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace WakaTime
{
    public class Dependencies
    {
        private const string CurrentPythonVersion = "3.5.1";
        private static string PythonBinaryLocation { get; set; }
        private static string PythonDownloadUrl
        {
            get
            {
                var arch = ProcessorArchitectureHelper.Is64BitOperatingSystem ? "amd64" : "win32";
                return string.Format("https://www.python.org/ftp/python/{0}/python-{0}-embed-{1}.zip", CurrentPythonVersion, arch);
            }
        }
        private static string AppDataDirectory {
            get
            {
                string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = Path.Combine(roamingFolder, "WakaTime");

                // Create folder if it does not exist
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                return appFolder;
            }
        }
        internal static string CliLocation
        {
            get
            {
                return Path.Combine(AppDataDirectory, Constants.CliFolder);
            }
        }

        static public void DownloadAndInstallCli()
        {
            Logger.Debug("Downloading wakatime-cli...");
            var url = Constants.CliUrl;
            var destinationDir = AppDataDirectory;
            var localZipFile = Path.Combine(destinationDir, "wakatime-cli.zip");

            // Download wakatime-cli
            var proxy = WakaTimePackage.GetProxy();
            var client = new WebClient { Proxy = proxy };
            client.DownloadFile(url, localZipFile);
            Logger.Debug("Finished downloading wakatime-cli.");

            // Remove old folder if it exists
            RecursiveDelete(Path.Combine(destinationDir, "wakatime-master"));

            // Extract wakatime-cli zip file
            Logger.Debug(string.Format("Extracting wakatime-cli to: {0}", destinationDir));
            ZipFile.ExtractToDirectory(localZipFile, destinationDir);
            Logger.Debug("Finished extracting wakatime-cli.");

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }

        static public void DownloadAndInstallPython()
        {
            Logger.Debug("Downloading python...");
            var url = PythonDownloadUrl;
            var destinationDir = AppDataDirectory;
            var localZipFile = Path.Combine(destinationDir, "python.zip");
            var extractToDir = Path.Combine(destinationDir, "python");

            // Download python
            var proxy = WakaTimePackage.GetProxy();
            var client = new WebClient { Proxy = proxy };
            client.DownloadFile(url, localZipFile);
            Logger.Debug("Finished downloading python.");

            // Remove old python folder if it exists
            RecursiveDelete(extractToDir);

            // Extract wakatime cli zip file
            Logger.Debug(string.Format("Extracting python to: {0}", extractToDir));
            ZipFile.ExtractToDirectory(localZipFile, extractToDir);
            Logger.Debug("Finished extracting python.");

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }

        internal static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        internal static string GetPython()
        {
            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetEmbeddedPythonPath();

            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetPythonPathFromMicrosoftRegistry();

            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetPythonPathFromFixedPath();

            return PythonBinaryLocation;
        }

        internal static string GetPythonPathFromMicrosoftRegistry()
        {
            try
            {
                var regex = new Regex(@"""([^""]*)\\([^""\\]+(?:\.[^"".\\]+))""");
                var pythonKey = Registry.ClassesRoot.OpenSubKey(@"Python.File\shell\open\command");
                if (pythonKey == null)
                    return null;

                var python = pythonKey.GetValue(null).ToString();
                var match = regex.Match(python);

                if (!match.Success) return null;

                var directory = match.Groups[1].Value;
                var fullPath = Path.Combine(directory, "pythonw");
                var process = new RunProcess(fullPath, "--version");

                process.Run();

                if (!process.Success)
                    return null;

                Logger.Debug(string.Format("Python found from Microsoft Registry: {0}", fullPath));

                return fullPath;
            }
            catch (Exception ex)
            {
                Logger.Error("GetPathFromMicrosoftRegistry:", ex);
                return null;
            }
        }

        internal static string GetPythonPathFromFixedPath()
        {
            List<string> locations = new List<string>();
            for (int i = 26; i <= 50; i++)
            {
                locations.Add(Path.Combine("\\python" + i, "pythonw"));
                locations.Add(Path.Combine("\\Python" + i, "pythonw"));
            }

            foreach (var location in locations)
            {
                try
                {
                    var process = new RunProcess(location, "--version");
                    process.Run();

                    if (!process.Success) continue;
                }
                catch { /*ignored*/ }

                Logger.Debug(string.Format("Python found by Fixed Path: {0}", location));

                return location;
            }

            return null;
        }

        internal static string GetEmbeddedPythonPath()
        {
            var path = Path.Combine(AppDataDirectory, "python", "pythonw");
            try
            {
                var process = new RunProcess(path, "--version");
                process.Run();

                if (!process.Success)
                    return null;

                Logger.Debug(string.Format("Python found from embedded location: {0}", path));

                return path;
            }
            catch (Exception ex)
            {
                Logger.Error("GetEmbeddedPath:", ex);
                return null;
            }
        }

        internal static bool DoesCliExist()
        {
            return File.Exists(CliLocation);
        }

        internal static bool IsCliUpToDate()
        {
            var process = new RunProcess(Dependencies.GetPython(), CliLocation, "--version");
            process.Run();

            if (process.Success)
            {
                var currentVersion = process.Error.Trim();
                Logger.Info(string.Format("Current wakatime-cli version is {0}", currentVersion));

                Logger.Info("Checking for updates to wakatime-cli...");
                var latestVersion = Constants.LatestWakaTimeCliVersion();

                if (currentVersion.Equals(latestVersion))
                {
                    Logger.Info("wakatime-cli is up to date.");
                    return true;
                }

                Logger.Info(string.Format("Found an updated wakatime-cli v{0}", latestVersion));
            }
            return false;
        }

        internal static void RecursiveDelete(string folder)
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch { /* ignored */ }
            try
            {
                File.Delete(folder);
            }
            catch { /* ignored */ }
        }
    }
}
