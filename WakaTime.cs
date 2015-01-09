using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace WakaTime.WakaTime {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidWakaTimePkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    public sealed class WakaTime : Package {
        private const int HeartbeatInterval = 2 * 60 * 1000; // 2 minute in milli seconds

        UtilityManager _utilityManager = UtilityManager.Instance;
        private EnvDTE.DTE _objDTE = null;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private string _lastFileSent = string.Empty;
        private DateTime _lastTimeSent = DateTime.Parse("01/01/1970 00:00:00");

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        
        public WakaTime() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        
        protected override void Initialize() {
            IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            Logger.Instance.initialize(log);
            try {
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
                base.Initialize();

                // IVsExtensionManager manager = GetService(typeof(SVsExtensionManager)) as IVsExtensionManager;
                // var extension = manager.GetInstalledExtensions().Where(n => n.Header.Name == "WakaTime").SingleOrDefault();
                // Version currentVersion = extension.Header.Version;

                // Check for Python, Wakatime utility and Api Key
                _utilityManager.initialize();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs) {
                    // Create the command for the menu item.
                    CommandID menuCommandID = new CommandID(GuidList.guidWakaTimeCmdSet, (int)PkgCmdIDList.cmdidUpdateApiKey);
                    MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                    mcs.AddCommand(menuItem);
                }

                bool isApiKeyFound = checkForApiKey();
                if (isApiKeyFound) {
                    initializeEvents();
                }
            } catch(Exception ex) {
                Logger.Instance.error(ex.Message);
            }
        }
        
        /// <summary>
        /// Send file on switching document.
        /// </summary>
        /// <param name="gotFocus">Activated window</param>
        /// <param name="lostFocus">Deactivated window</param>
        public void Window_Activated(Window gotFocus, Window lostFocus) {
            try {
                Document document = _objDTE.ActiveWindow.Document;
                if (document != null) {
                    sendFileToWakatime(document.FullName, false);
                }
            } catch(Exception ex) {
                Logger.Instance.error("Window_Activated : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is opened.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentOpened(EnvDTE.Document document) {
            try {
                sendFileToWakatime(document.FullName, false);
            } catch (Exception ex) {
                Logger.Instance.error("DocumentEvents_DocumentOpened : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is saved.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentSaved(EnvDTE.Document document) {
            try {
                sendFileToWakatime(document.FullName, true);
            } catch(Exception ex) {
                Logger.Instance.error("DocumentEvents_DocumentSaved : " + ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e) {
            try {
                displayApiKeyDialog();
            } catch(Exception ex) {
                Logger.Instance.error("MenuItemCallback : " + ex.Message);
            }
        }

        private bool checkForApiKey() {
            if (string.IsNullOrWhiteSpace(_utilityManager.ApiKey)) { // If key does not exist then prompt user to enter key
                DialogResult result = displayApiKeyDialog();
                if (result == DialogResult.Cancel) { //Otherwise it is assumed that user has entered some key.
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Initialize events and create timer
        /// </summary>
        private void initializeEvents() {
            if (_objDTE == null) {
                //Initialize events for file open/switch/save.
                _objDTE = (DTE)GetService(typeof(DTE));
                _docEvents = _objDTE.Events.DocumentEvents;
                _windowEvents = _objDTE.Events.WindowEvents;

                _docEvents.DocumentOpened += new _dispDocumentEvents_DocumentOpenedEventHandler(DocumentEvents_DocumentOpened);
                _docEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(DocumentEvents_DocumentSaved);
                _windowEvents.WindowActivated += new _dispWindowEvents_WindowActivatedEventHandler(Window_Activated);
            }
        }

        /// <summary>
        /// Display Api Key dialog box.
        /// </summary>
        private DialogResult displayApiKeyDialog() {
            APIKeyForm form = new APIKeyForm();
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK) {
                initializeEvents(); //If during plgin initialization user does not eneter key then this time 'events' should be initialized
            }

            return result;
        }

        /// <summary>
        /// Send file with absolute path to wakatime and store same in _lastFileSent
        /// </summary>
        /// <param name="fileName"></param>
        private void sendFileToWakatime(string fileName, bool isWrite) {
            DateTime now = DateTime.UtcNow;
            TimeSpan minutesSinceLastSent = now - _lastTimeSent;
            if (fileName != _lastFileSent || isWrite || minutesSinceLastSent.Minutes >= 2) {
                string projectName = _objDTE.Solution != null && !string.IsNullOrWhiteSpace(_objDTE.Solution.FullName) ? _objDTE.Solution.FullName : null;
                if (!string.IsNullOrWhiteSpace(projectName)) {
                    projectName = Path.GetFileNameWithoutExtension(projectName);
                }
                _utilityManager.sendFile(fileName, projectName, isWrite, _objDTE.Version);
                _lastFileSent = fileName;
                _lastTimeSent = now;
            }
        }

    }
}
