using System;
using System.Text;

namespace WakaTime
{
    class Config
    {
        public static string GetApiKey()
        {
            var keyValue = new StringBuilder(255);
            var configFilepath = GetConfigFilePath();
            if (!string.IsNullOrWhiteSpace(configFilepath) &&
                NativeMethods.GetPrivateProfileString("settings", "api_key", "", keyValue, 255, configFilepath) > 0)
                return keyValue.ToString();

            return string.Empty;
        }

        public static void SetApiKey(string value)
        {
            var configFilepath = GetConfigFilePath();
            if (!string.IsNullOrWhiteSpace(value))
                NativeMethods.WritePrivateProfileString("settings", "api_key", value, configFilepath);
        }

        public static string GetProxy()
        {
            var proxy = new StringBuilder(255);
            var configFilepath = GetConfigFilePath();
            if (!string.IsNullOrWhiteSpace(configFilepath) &&
                NativeMethods.GetPrivateProfileString("settings", "proxy", "", proxy, 255, configFilepath) > 0)
                return proxy.ToString();

            return string.Empty;
        }

        public static void SetProxy(string value)
        {
            var configFilepath = GetConfigFilePath();
            NativeMethods.WritePrivateProfileString("settings", "proxy", value, configFilepath);
        }

        public static string GetConfigFilePath()
        {
            var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userHomeDir) == false)
                return userHomeDir + "\\.wakatime.cfg";

            return null;
        }
    }
}