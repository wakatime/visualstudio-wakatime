using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace WakaTime.WakaTime {
    class Downloader {
        /// <summary>
        /// Download to current working directory
        /// </summary>
        /// <param name="url"></param>
        /// <param name="utilityName"></param>
        static public void downloadUtility(string url, string utilityName) {
            WebClient client = new WebClient();
            string currentDir = getCurrentDirectory();
            string fileToDownload = currentDir + "\\utility.zip";
            // Download utility
            client.DownloadFile(url, fileToDownload);

            //Extract to some temp folder
            string extractFolderName = currentDir + "\\utility";
            ZipFile.ExtractToDirectory(fileToDownload, extractFolderName);

            //search command line utility and copy
            string utilityFullpath = searchFile(extractFolderName, utilityName);
            if (string.IsNullOrWhiteSpace(utilityFullpath) == false) {
                File.Copy(utilityFullpath, currentDir + "\\" + utilityName, true);
            }

            //copy 'wakatime' folder
            string folderPath = searchFolder(extractFolderName, "wakatime");
            if (string.IsNullOrWhiteSpace(folderPath) == false) {
                DirectoryCopy(folderPath, currentDir + "\\wakatime", true);
            }
        }

        /// <summary>
        /// Download Python
        /// </summary>
        /// <param name="url"></param>
        /// <param name="utilityName"></param>
        static public void downloadPython(string url, string utilityName) {
            string currentDir = getCurrentDirectory();
            string fileToDownload = currentDir + "\\python.msi";

            WebClient client = new WebClient();
            client.DownloadFile(url, fileToDownload);

            string installDir = currentDir + "\\PythonInstall";
            string arguments = "/i \"" + fileToDownload + "\" " + "TARGETDIR=\"" + installDir + "\" /norestart /quiet";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = "msiexec";
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            var proc = Process.Start(procInfo);
            Thread.Sleep(60000);// waiting to complete python download

            addPathToEnvironmentVariable(installDir);
        }

        static public void addPathToEnvironmentVariable(string pathToAppend) {
            String oldPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine).Trim();
            string newPath = oldPath;
            if (oldPath.Length > 0)
            {
                if (oldPath[oldPath.Length - 1] != ';')
                {
                    newPath += ';';
                }
            }
            newPath += (pathToAppend + ';');

            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("PATH", newPath);
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
