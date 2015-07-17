using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace WakaTime
{
    class Downloader
    {
        static public void DownloadCli(string url, string dir)
        {
            Logger.Instance.Info("Downloading wakatime cli...");

            var client = new WebClient();
            var localZipFile = dir + "\\wakatime-cli.zip";

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);

            Logger.Instance.Info("Finished downloading wakatime cli.");

            Logger.Instance.Info("Extracting wakatime cli: " + dir.ToString());

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, dir);

            Logger.Instance.Info("Finished extracting wakatime cli.");
        }

        static public void DownloadPython(string url, string dir)
        {
            var localFile = dir + "\\python.msi";

            Logger.Instance.Info("Downloading python...");

            var client = new WebClient();
            client.DownloadFile(url, localFile);

            Logger.Instance.Info("Finished downloading python.");

            var arguments = "/i \"" + localFile + "\"";
            arguments = arguments + " /norestart /qb!";

            var procInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                FileName = "msiexec",
                CreateNoWindow = true,
                Arguments = arguments
            };

            Logger.Instance.Info("Installing python...");

            Process.Start(procInfo);

            Logger.Instance.Info("Finished installing python.");
        }
    }
}