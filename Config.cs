using System;
using System.Text;
using System.Runtime.InteropServices;

namespace WakaTime
{
    class Config
    {
        public static string getApiKey()
        {
            StringBuilder keyValue = new StringBuilder(255);
            string configFilepath = getConfigFilePath();
            if (string.IsNullOrWhiteSpace(configFilepath) == false)
            {
                if (NativeMethods.GetPrivateProfileString("settings", "api_key", "", keyValue, 255, configFilepath) > 0)
                {
                    return keyValue.ToString();
                }
            }

            return null;
        }

        public static void setApiKey(string apiKey)
        {
            string configFilepath = getConfigFilePath();
            if (string.IsNullOrWhiteSpace(apiKey) == false)
            {
                NativeMethods.WritePrivateProfileString("settings", "api_key", apiKey, configFilepath);
            }
        }

        public static string getConfigFilePath()
        {
            string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userHomeDir) == false)
            {
                return userHomeDir + "\\.wakatime.cfg";
            }

            return null;
        }
    }
}