using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WakaTime.Shared.ExtensionUtils;

namespace WakaTime.Forms
{
    public partial class ApiKeyForm : Form
    {
        private readonly ConfigFile _configFile;
        private readonly ILogger _logger;

        public ApiKeyForm(ConfigFile configFile, ILogger logger)
        {
            _configFile = configFile;
            _logger = logger;

            InitializeComponent();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _configFile.GetSetting("api_key");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            try
            {
                var matched = Regex.IsMatch(txtAPIKey.Text.Trim(), "(?im)^(waka_)?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}$");

                if (matched)
                {
                    _configFile.SaveSetting("settings", "api_key", txtAPIKey.Text.Trim());
                }
                else
                {
                    MessageBox.Show("Please enter valid Api Key.");

                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving data from ApiKeyForm: {ex}");

                MessageBox.Show(ex.Message);
            }
        }
    }
}
