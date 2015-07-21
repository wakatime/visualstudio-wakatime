using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace WakaTime
{
    static class Logger
    {
        private static IVsOutputWindowPane _wakatimeOutputWindowPane;

        private static IVsOutputWindowPane WakatimeOutputWindowPane
        {
            get { return _wakatimeOutputWindowPane ?? (_wakatimeOutputWindowPane = GetWakatimeOutputWindowPane()); }
        }

        private static IVsOutputWindowPane GetWakatimeOutputWindowPane()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null) return null;

            var outputPaneGuid = new Guid(GuidList.GuidWakatimeOutputPane.ToByteArray());
            IVsOutputWindowPane windowPane;

            outputWindow.CreatePane(ref outputPaneGuid, "Wakatime", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out windowPane);

            return windowPane;
        }
        
        internal static void ExceptionWriteLine(string message, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", message, ex);

            WriteLine("Handled Exception", exceptionMessage);
        }

        internal static void WarningWriteLine(string message)
        {
            WriteLine("Warning", message);
        }

        internal static void InfoWriteLine(string message)
        {
            WriteLine("Info", message);
        }

        private static void WriteLine(string category, string message)
        {
            var outputWindowPane = WakatimeOutputWindowPane;
            if (outputWindowPane == null) return;

            var outputMessage = string.Format("[Wakatime {0} {1}] {2}{3}", category,
                DateTime.Now.ToString("hh:mm:ss tt"), message, Environment.NewLine);

            outputWindowPane.OutputString(outputMessage);
        }
    }
}
