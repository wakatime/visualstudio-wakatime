﻿using System;
using System.Text;

namespace WakaTime
{
    class WakaTimeConfigFile
    {
        internal string ApiKey { get; set; }
        internal string Proxy { get; set; }
        internal string WorkPlace { get; set; }
        internal bool Debug { get; set; }

        private readonly string _configFilepath;

        internal WakaTimeConfigFile()
        {
            _configFilepath = GetConfigFilePath();
            Read();
        }

        internal void Read()
        {
            var ret = new StringBuilder(255);

            if (NativeMethods.GetPrivateProfileString("settings", "api_key", "", ret, 255, _configFilepath) > 0)
                ApiKey = ret.ToString();

            if (NativeMethods.GetPrivateProfileString("settings", "proxy", "", ret, 255, _configFilepath) > 0)
                Proxy = ret.ToString();

            if (NativeMethods.GetPrivateProfileString("settings", "workplace", "", ret, 255, _configFilepath) > 0)
                WorkPlace = ret.ToString();

            // ReSharper disable once InvertIf
            if (NativeMethods.GetPrivateProfileString("settings", "debug", "", ret, 255, _configFilepath) > 0)
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
            if (!string.IsNullOrEmpty(Proxy))
                NativeMethods.WritePrivateProfileString("settings", "proxy", Proxy.Trim(), _configFilepath);
            if (!string.IsNullOrEmpty(WorkPlace))
                NativeMethods.WritePrivateProfileString("settings", "workplace", WorkPlace.Trim(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "debug", Debug.ToString().ToLower(), _configFilepath);
        }

        static string GetConfigFilePath()
        {
            var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return userHomeDir + "\\.wakatime.cfg";
        }
    }
}