using System;
using System.IO;
using System.Text;

namespace WakaTime
{
    public class WakaTimeConfigFile
    {
        internal string ApiKey { get; set; }        
        internal string Proxy { get; set; }
        internal bool Debug { get; set; }

        private readonly string _configFilepath;

        internal WakaTimeConfigFile()
        {
            _configFilepath = GetConfigFilePath();
            Read();
        }

        internal void Read()
        {
            var ret = new StringBuilder(2083);

            ApiKey = NativeMethods.GetPrivateProfileString("settings", "api_key", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;

            Proxy = NativeMethods.GetPrivateProfileString("settings", "proxy", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;

            // ReSharper disable once InvertIf
            if (NativeMethods.GetPrivateProfileString("settings", "debug", "", ret, 2083, _configFilepath) > 0)
            {
                bool debug;
                if (bool.TryParse(ret.ToString(), out debug))
                    Debug = debug;
            }
        }

        internal void Save()
        {
            if (!string.IsNullOrEmpty(ApiKey))
                NativeMethods.WritePrivateProfileString("settings", "api_key", ApiKey.Trim(), _configFilepath);

            NativeMethods.WritePrivateProfileString("settings", "proxy", Proxy.Trim(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "debug", Debug.ToString().ToLower(), _configFilepath);
        }

        static string GetConfigFilePath()
        {
            return Path.Combine(WakaTimeConstants.UserConfigDir, ".wakatime.cfg");
        }
    }
}