using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace WakaTime
{
    class RunProcess
    {
        private readonly string _program;
        private readonly string[] _arguments;
        private bool _captureOutput;
        private readonly string _workingDir;

        internal RunProcess(string program, string workingDir, params string[] arguments)
        {
            _program = program;
            _arguments = arguments;
            _workingDir = workingDir;
            _captureOutput = true;
        }

        internal void RunInBackground()
        {
            _captureOutput = false;
            Run();
        }

        internal string Output { get; private set; }

        internal string Error { get; private set; }

        internal bool Success
        {
            get { return Exception == null; }
        }

        internal Exception Exception { get; private set; }

        internal void Run()
        {
            try
            {
                var procInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardError = _captureOutput,
                    RedirectStandardOutput = _captureOutput,
                    FileName = _program,
                    CreateNoWindow = true,
                    Arguments = GetArgumentString()
                };
                if (!string.IsNullOrEmpty(_workingDir))
                {
                    procInfo.WorkingDirectory = _workingDir;
                }

                using (var process = Process.Start(procInfo))
                {
                    if (_captureOutput)
                    {

                        var stdOut = new StringBuilder();
                        var stdErr = new StringBuilder();

                        while (process != null && !process.HasExited)
                        {
                            stdOut.Append(process.StandardOutput.ReadToEnd());
                            stdErr.Append(process.StandardError.ReadToEnd());
                        }

                        if (process != null)
                        {
                            stdOut.Append(process.StandardOutput.ReadToEnd());
                            stdErr.Append(process.StandardError.ReadToEnd());
                        }

                        Output = stdOut.ToString().Trim(Environment.NewLine.ToCharArray()).Trim('\r', '\n');
                        Error = stdErr.ToString().Trim(Environment.NewLine.ToCharArray()).Trim('\r', '\n');
                    }

                    Exception = null;
                }
            }
            catch (Exception ex)
            {
                Output = null;
                Error = null;
                Exception = ex;
            }
        }

        private string GetArgumentString()
        {
            var args = _arguments.Aggregate("", (current, arg) => current + "\"" + arg + "\" ");
            return args.TrimEnd(' ');
        }
    }
}