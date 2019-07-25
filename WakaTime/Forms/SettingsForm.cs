using System;
using System.Windows.Forms;
using WakaTime.Shared.ExtensionUtils;

namespace WakaTime.Forms
{
    public partial class SettingsForm : Form
    {
        internal event EventHandler ConfigSaved;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = WakaTimePackage.Config.ApiKey;
                txtProxy.Text = WakaTimePackage.Config.Proxy;
                chkDebugMode.Checked = WakaTimePackage.Config.Debug;
            }
            catch (Exception ex)
            {
                Logger.Error("Error when loading form SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                var parse = Guid.TryParse(txtAPIKey.Text.Trim(), out var apiKey);         
                                     
                if (parse)
                {
                    WakaTimePackage.Config.ApiKey = apiKey.ToString();
                    WakaTimePackage.Config.Proxy = txtProxy.Text.Trim();
                    WakaTimePackage.Config.Debug = chkDebugMode.Checked;
                    WakaTimePackage.Config.Save();
                    OnConfigSaved();
                }
                else
                {
                    MessageBox.Show(@"Please enter valid Api Key.");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error when saving data from SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void OnConfigSaved()
        {
            var handler = ConfigSaved;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
