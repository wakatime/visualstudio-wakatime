using Microsoft.VisualStudio.Shell;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WakaTime.ExtensionUtils
{
    internal class StatusbarControl : TextBlock
    {
        private const string Icon = "🕑";

        private readonly Brush _normalBackground = new SolidColorBrush(Colors.Transparent);
        private readonly Brush _hoverBackground = new SolidColorBrush(Colors.White) { Opacity = 0.2 };

        public StatusbarControl()
        {
            Text = Icon;
            Foreground = new SolidColorBrush(Colors.White);
            Background = _normalBackground;

            VerticalAlignment = VerticalAlignment.Center;
            Margin = new Thickness(7, 0, 7, 0);
            Padding = new Thickness(7, 0, 7, 0);

            MouseEnter += (s, e) =>
            {
                Cursor = Cursors.Hand;
                Background = _hoverBackground;
            };

            MouseLeave += (s, e) =>
            {
                Cursor = Cursors.Arrow;
                Background = _normalBackground;
            };

            MouseLeftButtonUp += (s, e) =>
            {
                // Open WakaTime in browser
                System.Diagnostics.Process.Start("https://wakatime.com/");
            };
        }

        public void SetText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Text = string.IsNullOrEmpty(text) ? Icon : $"{Icon} {text}";
        }

        public void SetToolTip(string toolTip)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ToolTip = toolTip;
        }
    }
}
