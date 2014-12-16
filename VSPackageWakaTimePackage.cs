using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Threading;

namespace Wakatime.VSPackageWakaTime
{
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
    [Guid(GuidList.guidVSPackageWakaTimePkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    public sealed class VSPackageWakaTimePackage : Package
    {
        private const int HeartbeatInterval = 2 * 60 * 1000; // 2 minute in milli seconds

        WakatimeUtilityManager _utilityManager = WakatimeUtilityManager.Instance;
        private EnvDTE.DTE _objDTE = null;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private string _lastFileSent = string.Empty; //Last heartbeat sent to Wakatime
        private System.Threading.Timer _heartbeatTimer;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSPackageWakaTimePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
                base.Initialize();
                IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                VSWakatimeLogger.Instance.initialize(log);

                // Check for Python, Wakatime utility and Api Key
                _utilityManager.initialize();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (null != mcs)
                {
                    // Create the command for the menu item.
                    CommandID menuCommandID = new CommandID(GuidList.guidVSPackageWakaTimeCmdSet, (int)PkgCmdIDList.cmdidUpdateAppKey);
                    MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                    mcs.AddCommand(menuItem);
                }

                bool isApiKeyFound = checkForApiKey();
                if (isApiKeyFound)
                {
                    initializeEvents();
                }
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog(ex.Message);
            }
        }

        /// <summary>
        /// Send file on switching document.
        /// </summary>
        /// <param name="gotFocus">Activated window</param>
        /// <param name="lostFocus">Deactivated window</param>
        public void Window_Activated(Window gotFocus, Window lostFocus)
        {
            try
            {
                Document activeDoc = _objDTE.ActiveWindow.Document;
                if (activeDoc != null)
                {
                    sendFileToWakatime(activeDoc.FullName);
                }
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("Window_Activated : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is opened.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentOpened(EnvDTE.Document document)
        {
            try
            {
                sendFileToWakatime(document.FullName);
            }
            catch (Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("DocumentEvents_DocumentOpened : " + ex.Message);
            }
        }

        /// <summary>
        /// Called when any document is saved.
        /// </summary>
        /// <param name="document"></param>
        public void DocumentEvents_DocumentSaved(EnvDTE.Document document)
        {
            try
            {
                _utilityManager.sendFile(document.FullName, " --write");  // No need to compare previous heartbeat in case of save
                _lastFileSent = document.FullName;
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("DocumentEvents_DocumentSaved : " + ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                displayApiKeyDialog();
            }
            catch(Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("MenuItemCallback : " + ex.Message);
            }
        }

        private bool checkForApiKey()
        {
            if (string.IsNullOrWhiteSpace(_utilityManager.ApiKey)) // If key does not exist then prompt user to enter key
            {
                DialogResult result = displayApiKeyDialog();
                if (result == DialogResult.Cancel)//Otherwise it is assumed that user has entered some key.
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Initialize events and create timer
        /// </summary>
        private void initializeEvents()
        {
            if (_objDTE == null)
            {
                //Initialize events for file open/switch/save.
                _objDTE = (DTE)GetService(typeof(DTE));
                _docEvents = _objDTE.Events.DocumentEvents;
                _windowEvents = _objDTE.Events.WindowEvents;

                _docEvents.DocumentOpened += new _dispDocumentEvents_DocumentOpenedEventHandler(DocumentEvents_DocumentOpened);
                _docEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(DocumentEvents_DocumentSaved);
                _windowEvents.WindowActivated += new _dispWindowEvents_WindowActivatedEventHandler(Window_Activated);
            }

            // Create heartbeat timer
            createHeartbeatTimer();
        }

        /// <summary>
        /// Display Api Key dialog box.
        /// </summary>
        private DialogResult displayApiKeyDialog()
        {
            APIKeyForm form = new APIKeyForm();
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                initializeEvents(); //If during plgin initialization user does not eneter key then this time 'events' should be initialized
            }

            return result;
        }

        /// <summary>
        /// Send file with absolute path to wakatime and store same in _lastFileSent
        /// </summary>
        /// <param name="fileName"></param>
        private void sendFileToWakatime(string fileName)
        {
            if (fileName != _lastFileSent)
            {
                _utilityManager.sendFile(fileName);
                _lastFileSent = fileName;

                _heartbeatTimer.Change(HeartbeatInterval, HeartbeatInterval); // Extend timer by another 2 minutes.
            }
        }

        /// <summary>
        /// Timer to send heartbeat in every 2 minutes if no file selection has been changed.
        /// </summary>
        private void createHeartbeatTimer()
        {
            if (_heartbeatTimer == null)
            {
                _heartbeatTimer = new System.Threading.Timer(new TimerCallback(HeartbeatTimerCallBack), null, HeartbeatInterval, HeartbeatInterval);
            }
        }

        /// <summary>
        /// Heartbeat call back fired in every 2 minutes to send active file.
        /// </summary>
        /// <param name="state"></param>
        private void HeartbeatTimerCallBack(object state)
        {
            try
            {
                if (_objDTE.ActiveDocument != null)
                {
                    _utilityManager.sendFile(_objDTE.ActiveDocument.FullName); // No need to compare previous heartbeat
                    _lastFileSent = _objDTE.ActiveDocument.FullName;
                }
            }
            catch (Exception ex)
            {
                VSWakatimeLogger.Instance.writeToLog("HeartbeatTimerCallBack : " + ex.Message);
            }
        }
    }
}
