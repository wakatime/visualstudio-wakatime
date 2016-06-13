using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace WakaTime
{
    internal class PythonCliParameters
    {
        private string Cli
        {
            get { return Dependencies.CliLocation; }
        }
        public string Key { get; set; }
        public string File { get; set; }
        public string Time { get; set; }
        public string Plugin { get; set; }
        public bool IsWrite { get; set; }
        public string Project { get; set; }
        public bool HasExtraHeartbeats { get; set; }

        public string[] ToArray(bool obfuscate = false)
        {
            var parameters = new Collection<string>
            {
                Cli,
                "--key",
                obfuscate ? string.Format("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXX{0}", Key.Substring(Key.Length - 4)) : Key,
                "--entity",
                File,
                "--time",
                Time.ToString(),
                "--plugin",
                Plugin
            };

            if (IsWrite)
                parameters.Add("--write");

            // ReSharper disable once InvertIf
            if (!string.IsNullOrEmpty(Project))
            {
                parameters.Add("--project");
                parameters.Add(Project);
            }
            
            if (HasExtraHeartbeats)
                parameters.Add("--extra-heartbeats");

            return parameters.ToArray();
        }
    }
}
