using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using WakaTime.Forms;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using WakaTime.Shared.ExtensionUtils;
using WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers;
using Configuration = WakaTime.Shared.ExtensionUtils.Configuration;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;
using PackageAutoLoadFlags = WakaTime.Shared.ExtensionUtils.AsyncPackageHelpers.PackageAutoLoadFlags;

namespace WakaTime
{
    [Guid(GuidList.GuidWakaTimePkgString)]
    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Shared.ExtensionUtils.AsyncPackageHelpers.ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F", PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WakaTimePackage : Package, IAsyncLoadablePackageInitialize
    {
        #region Fields
        internal static ConfigFile Config;
        private static SettingsForm _settingsForm;

        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;

        public static DTE ObjDte;

        private static string _solutionName = string.Empty;

        private Shared.ExtensionUtils.WakaTime _wakaTime;
        #endregion

        #region Startup/Cleanup        
        protected override void Initialize()
        {
            base.Initialize();
            ObjDte = (DTE)GetService(typeof(DTE));

            var configuration = new Configuration
            {
                EditorName = "visualstudio",
                PluginName = "visualstudio-wakatime",
                EditorVersion = ObjDte == null ? string.Empty : ObjDte.Version
            };
            _wakaTime = new Shared.ExtensionUtils.WakaTime(this, configuration, new Logger());

            // Only perform initialization if async package framework not supported
            if (_wakaTime.IsAsyncLoadSupported) return;

            // Try force initializing in brackground
            _wakaTime.Logger.Debug("Initializing in background thread.");
            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        public IVsTask Initialize(IAsyncServiceProvider pServiceProvider, IProfferAsyncService pProfferService,
            IAsyncProgressCallback pProgressCallback)
        {
            if (!_wakaTime.IsAsyncLoadSupported)
            {
                throw new InvalidOperationException("Async Initialize method should not be called when async load is not supported.");
            }

            return ThreadHelper.JoinableTaskFactory.RunAsync<object>(async () =>
            {
                _wakaTime.Logger.Debug("Initializing async.");
                InitializeAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                OnOnStartupComplete();

                return null;
            }).AsVsTask();
        }

        private void InitializeAsync()
        {
            try
            {
                // VisualStudio Object                
                _docEvents = ObjDte.Events.DocumentEvents;
                _windowEvents = ObjDte.Events.WindowEvents;
                _solutionEvents = ObjDte.Events.SolutionEvents;

                // Settings Form
                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;

                // Add our command handlers for menu (commands must exist in the .vsct file)
                if (GetService(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.UpdateWakaTimeSettings);
                    var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                    mcs.AddCommand(menuItem);
                }

                // setup event handlers
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _windowEvents.WindowActivated += WindowEventsOnWindowActivated;
                _solutionEvents.Opened += SolutionEventsOnOpened;

                _wakaTime.InitializeAsync();
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("Error Initializing WakaTime", ex);
            }
        }        
        #endregion

        #region Event Handlers
        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                _wakaTime.HandleActivity(document.FullName, false, GetProjectName());
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                _wakaTime.HandleActivity(document.FullName, true, GetProjectName());
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = ObjDte.ActiveWindow.Document;
                if (document != null)
                    _wakaTime.HandleActivity(document.FullName, false, GetProjectName());
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = ObjDte.Solution.FullName;
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("SolutionEventsOnOpened", ex);
            }
        }

        private void OnOnStartupComplete()
        {
            // Prompt for api key if not already set
            if (string.IsNullOrEmpty(_wakaTime.Config.ApiKey))
                PromptApiKey();
        }
        #endregion

        #region Methods

        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            Config.Read();
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                SettingsPopup();
            }
            catch (Exception ex)
            {
                _wakaTime.Logger.Error("MenuItemCallback", ex);
            }
        }

        private void PromptApiKey()
        {
            _wakaTime.Logger.Info("Please input your api key into the wakatime window.");
            var form = new ApiKeyForm();
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : (ObjDte.Solution != null && !string.IsNullOrEmpty(ObjDte.Solution.FullName))
                    ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName)
                    : string.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _docEvents.DocumentOpened -= DocEventsOnDocumentOpened;
            _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
            _windowEvents.WindowActivated -= WindowEventsOnWindowActivated;
            _solutionEvents.Opened -= SolutionEventsOnOpened;

            _wakaTime.Dispose();
        }
        #endregion        
    }
}
