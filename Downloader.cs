using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WakaTime
{
    public class Downloader
    {
        static public void DownloadCli(string url, string destinationDir)
        {
            Logger.Debug("Downloading wakatime cli...");

            // Check for proxy setting
            WebProxy proxy = WakaTimePackage.GetProxy();

            var localZipFile = destinationDir + "\\wakatime-cli.zip";

            var client = new WebClient();
            client.Proxy = proxy;

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);
            

            Logger.Debug("Finished downloading wakatime cli.");

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, destinationDir);

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }

        static public void DownloadAndInstallPython(string url, string destinationDir)
        {
            Logger.Debug("Downloading python...");

            // Check for proxy setting
            WebProxy proxy = WakaTimePackage.GetProxy();

            var localFile = destinationDir + "\\python.msi";

            var client = new WebClient();
            client.Proxy = proxy;
            client.DownloadFile(url, localFile);

            Logger.Debug("Finished downloading python.");

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

            Process.Start(procInfo);

            try
            {
                File.Delete(localFile);
            }
            catch { /* ignored */ }
        }

        
    }
}