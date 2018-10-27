using System;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigFile _wakaTimeConfigFile;
        internal event EventHandler ConfigSaved;

        public SettingsForm()
        {
            InitializeComponent();

            _wakaTimeConfigFile = new ConfigFile();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _wakaTimeConfigFile.ApiKey;
                txtProxy.Text = _wakaTimeConfigFile.Proxy;
                chkDebugMode.Checked = _wakaTimeConfigFile.Debug;
                chkDisableThreading.Checked = _wakaTimeConfigFile.DisableThreading;
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
                Guid apiKey;
                var parse = Guid.TryParse(txtAPIKey.Text.Trim(), out apiKey);         
                                     
                if (parse)
                {
                    _wakaTimeConfigFile.ApiKey = apiKey.ToString();
                    _wakaTimeConfigFile.Proxy = txtProxy.Text.Trim();
                    _wakaTimeConfigFile.Debug = chkDebugMode.Checked;
                    _wakaTimeConfigFile.DisableThreading = chkDisableThreading.Checked;
                    _wakaTimeConfigFile.Save();
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
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtProxy_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtAPIKey_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkDisableThreading_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
