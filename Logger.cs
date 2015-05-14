using System;
using Microsoft.VisualStudio.Shell.Interop;
using System.Globalization;

namespace WakaTime
{
    class Logger
    {
        // Singleton class for logging in Visul Studio default logger file ActivityLog.xml
        private static Logger _instance;
        private IVsActivityLog _log;

        public static Logger Instance
        {
            get { return _instance ?? (_instance = new Logger()); }
        }

        public void Initialize(IVsActivityLog log)
        {
            _log = log;
        }

        public void Error(string message)
        {
            _log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                ToString(),
                string.Format(CultureInfo.CurrentCulture,
                    "{0}", message));
        }

        public void Info(string message)
        {
            _log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                ToString(),
                string.Format(CultureInfo.CurrentCulture,
                    "{0}", message));
        }
    }
}
