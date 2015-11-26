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
            var proxy = WakaTimePackage.GetProxy();

            var localZipFile = destinationDir + "\\wakatime-cli.zip";

            var client = new WebClient { Proxy = proxy };

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
            var proxy = WakaTimePackage.GetProxy();

            var localFile = destinationDir + "\\python.zip";

            var client = new WebClient { Proxy = proxy };

            // Download embeddable python
            client.DownloadFile(url, localFile);

            Logger.Debug("Finished downloading python.");

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localFile, Path.Combine(destinationDir, "python"));

            Logger.Info(string.Format("Finished extracting python: {0}", destinationDir));

            try
            {
                File.Delete(localFile);
            }
            catch { /* ignored */ }
        }
    }
}