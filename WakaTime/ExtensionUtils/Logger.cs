using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;
using WakaTime.Shared.ExtensionUtils;

namespace WakaTime.ExtensionUtils
{
    public class Logger : ILogger
    {
        private readonly ConfigFile _config;
        private IVsOutputWindowPane _wakatimeOutputWindowPane;
        private IVsOutputWindowPane WakatimeOutputWindowPane =>
            _wakatimeOutputWindowPane ?? (_wakatimeOutputWindowPane = GetWakatimeOutputWindowPane());

        public Logger()
        {
            _config = new ConfigFile();
        }

        private static IVsOutputWindowPane GetWakatimeOutputWindowPane()
        {
            if (!(Package.GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow)) return null;

            var outputPaneGuid = new Guid(GuidList.GuidWakatimeOutputPane.ToByteArray());

            outputWindow.CreatePane(ref outputPaneGuid, "WakaTime", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out var windowPane);

            return windowPane;
        }

        public void Debug(string message)
        {
            if (!_config.Debug)
                return;

            Log(LogLevel.Debug, message);
        }

        public void Error(string message, Exception ex = null)
        {
            var exceptionMessage = $"{message}: {ex}";

            Log(LogLevel.HandledException, exceptionMessage);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        private void Log(LogLevel level, string message)
        {
            var outputWindowPane = WakatimeOutputWindowPane;
            if (outputWindowPane == null) return;

            var outputMessage =
                $"[WakaTime {Enum.GetName(level.GetType(), level)} {DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture)}] {message}{Environment.NewLine}";

            outputWindowPane.OutputString(outputMessage);
        }
    }
}
