using System;
using System.Windows.Forms;
using WakaTime.Shared.ExtensionUtils;

namespace WakaTime.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigFile _configFile;
        private readonly ILogger _logger;

        internal event EventHandler ConfigSaved;

        public SettingsForm(ConfigFile configFile, ILogger logger)
        {
            _configFile = configFile;
            _logger = logger;

            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _configFile.ApiKey;
                txtProxy.Text = _configFile.Proxy;
                chkDebugMode.Checked = _configFile.Debug;
            }
            catch (Exception ex)
            {
                _logger.Error("Error when loading form SettingsForm:", ex);

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
                    _configFile.ApiKey = apiKey.ToString();
                    _configFile.Proxy = txtProxy.Text.Trim();
                    _configFile.Debug = chkDebugMode.Checked;
                    _configFile.Save();

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
                _logger.Error($"Error saving data from SettingsForm: {ex}");

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
