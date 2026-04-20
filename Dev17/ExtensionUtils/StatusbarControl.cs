using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WakaTime.ExtensionUtils
{
    internal class StatusbarControl : TextBlock
    {
        private const string Icon = "🕑";
        private const string DefaultDashboardUrl = "https://wakatime.com/";

        private readonly Brush _normalBackground = new SolidColorBrush(Colors.Transparent);
        private readonly Brush _hoverBackground = new SolidColorBrush(Colors.White) { Opacity = 0.2 };
        private readonly string _dashboardUrl;

        public StatusbarControl(string apiUrl)
        {
            _dashboardUrl = GetDashboardUrl(apiUrl);
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
                System.Diagnostics.Process.Start(_dashboardUrl);
            };
        }

        private static string GetDashboardUrl(string apiUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
                return DefaultDashboardUrl;

            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri))
                return DefaultDashboardUrl;

            var host = apiUri.Host.StartsWith("api.", StringComparison.OrdinalIgnoreCase)
                ? apiUri.Host.Substring(4)
                : apiUri.Host;

            var dashboardUriBuilder = new UriBuilder(apiUri.Scheme, host, apiUri.IsDefaultPort ? -1 : apiUri.Port)
            {
                Path = "/"
            };

            return dashboardUriBuilder.Uri.AbsoluteUri;
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
