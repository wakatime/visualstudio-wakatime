using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace WakaTime.ExtensionUtils
{
    internal static class StatusbarInjector
    {
        private static Panel _panel;

        private static DependencyObject FindChild(DependencyObject parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                {
                    return frameworkElement;
                }

                child = FindChild(child, childName);

                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static async Task EnsureUIAsync()
        {
            while (_panel is null)
            {
                _panel = FindChild(Application.Current.MainWindow, "StatusBarPanel") as DockPanel;
                if (_panel is null)
                {
                    // Start window is showing. Need to wait for status bar render.
                    await Task.Delay(5000);
                }
            }
        }

        public static async Task InjectControlAsync(FrameworkElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await EnsureUIAsync();

            // Place the element to the left of all elements on the right side of the status bar
            element.SetValue(DockPanel.DockProperty, Dock.Right);
            int index = GetLastDockRightElementIndex(_panel.Children) + 1;
            _panel.Children.Insert(index, element);
        }

        private static int GetLastDockRightElementIndex(UIElementCollection elements)
        {
            int lastDockRightElementIndex = 0;

            int elementCount = elements.Count;
            for (int i = 0; i < elementCount; i++)
            {
                object element = elements[i];

                if (element is DependencyObject dependencyObject &&
                    dependencyObject.GetValue(DockPanel.DockProperty) is Dock dockProperty &&
                    dockProperty == Dock.Right)
                {
                    lastDockRightElementIndex = i;
                }
            }

            return lastDockRightElementIndex;
        }
    }
}
