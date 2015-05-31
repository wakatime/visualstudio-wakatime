using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WakaTime
{
    class RunProcess
    {
        private string _program;
        private string[] _arguments;
        private bool _captureOutput;
        private string _stdOut;
        private string _stdErr;
        private Exception _exception;

        public RunProcess(string program, params string[] arguments)
        {
            this._program = program;
            this._arguments = arguments;
            this._captureOutput = true;
        }

        public void RunInBackground()
        {
            this._captureOutput = false;
            this.Run();
        }

        public string Output()
        {
            return this._stdOut;
        }

        public string Error()
        {
            return this._stdErr;
        }

        public bool OK()
        {
            return this._exception == null;
        }

        public Exception Exception()
        {
            return this._exception;
        }

        public string ExceptionMessage()
        {
            return this._exception.Message;
        }

        public void Run()
        {
            try
            {
                var procInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardError = this._captureOutput,
                    RedirectStandardOutput = this._captureOutput,
                    FileName = this._program,
                    CreateNoWindow = true,
                    Arguments = GetArgumentString(),
                };
                using (var process = Process.Start(procInfo))
                {
                    if (this._captureOutput)
                    {

                        var stdOut = new StringBuilder();
                        var stdErr = new StringBuilder();

                        while (!process.HasExited)
                        {
                            stdOut.Append(process.StandardOutput.ReadToEnd());
                            stdErr.Append(process.StandardError.ReadToEnd());
                        }
                        stdOut.Append(process.StandardOutput.ReadToEnd());
                        stdErr.Append(process.StandardError.ReadToEnd());

                        this._stdOut = stdOut.ToString().Trim(Environment.NewLine.ToCharArray()).Trim('\r', '\n');
                        this._stdErr = stdErr.ToString().Trim(Environment.NewLine.ToCharArray()).Trim('\r', '\n');
                    }

                    this._exception = null;
                }
            }
            catch (Exception ex)
            {
                this._stdOut = null;
                this._stdErr = null;
                this._exception = ex;
            }
        }

        private string GetArgumentString()
        {
            string args = "";
            foreach (string arg in this._arguments)
            {
                args = args + "\"" + arg + "\" ";
            }
            return args.TrimEnd(' ');
        }
    }
}