using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WakaTime
{
    static class PythonManager
    {
        private const string CurrentPythonVersion = "3.4.3";
        private static string PythonBinaryLocation { get; set; }

        internal static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        internal static string CliPath
        {
            get { return @"wakatime-master\wakatime\cli.py"; }
        }

        internal static string GetPython()
        {
            if (PythonBinaryLocation != null)
                return PythonBinaryLocation;

            var path = TryGetPathFromMicrosoftRegister() ?? TryGetPathFromFixedPath();            

            return path;
        }

        static string TryGetPathFromMicrosoftRegister()
        {
            try
            {
                var regex = new Regex(@"""([^""]*)\\([^""\\]+(?:\.[^"".\\]+))""");
                var pythonKey = Registry.ClassesRoot.OpenSubKey(@"Python.File\shell\open\command");
                var python = pythonKey.GetValue(null).ToString();
                var match = regex.Match(python);

                if (!match.Success) return null;

                var directory = match.Groups[1].Value;
                var fullPath = Path.Combine(directory, "pythonw.exe");
                var process = new RunProcess(fullPath, "--version");

                process.Run();

                if (!process.Success || !process.Output.StartsWith("Python "))
                    return null;

                PythonBinaryLocation = fullPath;
                return fullPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string TryGetPathFromFixedPath()
        {
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

            foreach (var location in locations)
            {
                var process = new RunProcess(location, "--version");

                process.Run();

                if (!process.Success) continue;

                PythonBinaryLocation = location;
                return location;
            }

            return null;
        }

        internal static string GetPythonDownloadUrl()
        {
            var url = "https://www.python.org/ftp/python/" + CurrentPythonVersion + "/python-" + CurrentPythonVersion;

            if (ProcessorArchitectureHelper.Is64BitOperatingSystem)
                url = url + ".amd64";

            url = url + ".msi";

            return url;
        }

    }
}