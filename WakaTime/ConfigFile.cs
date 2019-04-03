using System;
using System.IO;
using System.Text;

namespace WakaTime
{
    public class ConfigFile
    {
        internal string ApiKey { get; set; }        
        internal string Proxy { get; set; }
        internal bool Debug { get; set; }
        internal bool DisableThreading { get; set; }

        private readonly string _configFilepath;

        internal ConfigFile()
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
                if (bool.TryParse(ret.ToString(), out var debug))
                    Debug = debug;
            }

            if (NativeMethods.GetPrivateProfileString("settings", "disable_threading", "", ret, 2083, _configFilepath) > 0)
            {
                if (bool.TryParse(ret.ToString(), out var disableThreading))
                    DisableThreading = disableThreading;
            }
        }

        internal void Save()
        {
            if (!string.IsNullOrEmpty(ApiKey))
                NativeMethods.WritePrivateProfileString("settings", "api_key", ApiKey.Trim(), _configFilepath);

            NativeMethods.WritePrivateProfileString("settings", "proxy", Proxy.Trim(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "debug", Debug.ToString().ToLower(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "disable_threading", DisableThreading.ToString().ToLower(), _configFilepath);
        }

        static string GetConfigFilePath()
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeFolder, ".wakatime.cfg");
        }
    }
}