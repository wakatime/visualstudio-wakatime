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

            return null;
        }

        public static void SetApiKey(string apiKey)
        {
            var configFilepath = GetConfigFilePath();
            if (string.IsNullOrWhiteSpace(apiKey) == false)            
                NativeMethods.WritePrivateProfileString("settings", "api_key", apiKey, configFilepath);            
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