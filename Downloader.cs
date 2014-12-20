using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace WakaTime.WakaTime {
    class Downloader {

        /// <summary>
        /// Download wakatime-cli
        /// </summary>
        /// <param name="url"></param>
        /// <param name="installDir"></param>
        static public void downloadCLI(string url, string installDir) {
            WebClient client = new WebClient();
            string currentDir = getCurrentDirectory();
            string fileToDownload = currentDir + "\\wakatime-cli.zip";

            // Download utility
            Logger.Instance.info("Downloader: downloading wakatime-cli...");
            client.DownloadFile(url, fileToDownload);

            //Extract to some temp folder
            Logger.Instance.info("Downloader: extracting wakatime-cli...");
            ZipFile.ExtractToDirectory(fileToDownload, installDir);
        }

        /// <summary>
        /// Download and install Python
        /// </summary>
        /// <param name="url"></param>
        /// <param name="installDir"></param>
        static public void downloadPython(string url, string installDir) {
            string fileToDownload = getCurrentDirectory() + "\\python.msi";

            WebClient client = new WebClient();
            Logger.Instance.info("Downloader: downloading python.msi...");
            client.DownloadFile(url, fileToDownload);

            string arguments = "/i \"" + fileToDownload + "\"";
            if (installDir != null) {
                arguments = arguments + " TARGETDIR=\"" + installDir + "\"";
            }
            arguments = arguments + " /norestart /qb!";
            
            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardError = true;
            procInfo.FileName = "msiexec";
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            Logger.Instance.info("Downloader: installing python...");
            var proc = Process.Start(procInfo);
            Logger.Instance.info("Downloader: finished installing python.");
        }

        static public string getCurrentDirectory() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assembly);

            return path;
        }

        /// <summary>
        /// Search command line utility from downloaded folder
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileToSearch"></param>
        /// <returns></returns>
        static public string searchFile(string dir, string fileToSearch) {
            foreach (string subDir in Directory.GetDirectories(dir))
            {
                foreach (string file in Directory.GetFiles(subDir, fileToSearch))
                {
                    if (file.Contains(fileToSearch))
                    {
                        return file;
                    }
                }
                searchFile(subDir, fileToSearch);
            }

            return null;
        }

        /// <summary>
        /// Search 'wakatime' folder from downloaded folder
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="serachDir"></param>
        /// <returns></returns>
        static public string searchFolder(string dir, string serachDir) {
            string [] directory = Directory.GetDirectories(dir, serachDir, SearchOption.AllDirectories);
            if (directory.Length > 0) {
                return directory[0];
            }

            return null;
        }

        /// <summary>
        /// Directory copy
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs) {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
