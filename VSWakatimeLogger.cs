using System;
using Microsoft.VisualStudio.Shell.Interop;
using System.Globalization;

namespace Wakatime.VSPackageWakaTime
{
    /// <summary>
    /// Singleton class for logging in Visula studio default logger file ActivityLog.xml
    /// </summary>
    class VSWakatimeLogger
    {
        private static VSWakatimeLogger _instance;
        private IVsActivityLog _log;

        public static VSWakatimeLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VSWakatimeLogger();
                }
                return _instance;
            }
        }

        public void initialize(IVsActivityLog log)
        {
            _log = log;
            int hr = log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                this.ToString(),
                string.Format(CultureInfo.CurrentCulture,
                "Entering initializer for: {0}", this.ToString()));
        }

        public void writeToLog(string message)
        {
            _log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                           this.ToString(),
                           string.Format(CultureInfo.CurrentCulture,
                                        "{0}", message));
        }
    }
}
