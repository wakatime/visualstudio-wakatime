using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;

namespace WakaTime
{
    class Downloader
    {

        static public void downloadCLI(string url, string dir)
        {
            WebClient client = new WebClient();
            string localZipFile = dir + "\\wakatime-cli.zip";

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, dir);
        }

        static public void downloadPython(string url, string dir)
        {
            string localFile = dir + "\\python.msi";

            WebClient client = new WebClient();
            client.DownloadFile(url, localFile);

            string arguments = "/i \"" + localFile + "\"";
            arguments = arguments + " /norestart /qb!";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardError = true;
            procInfo.FileName = "msiexec";
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            var proc = Process.Start(procInfo);
        }
    }
}