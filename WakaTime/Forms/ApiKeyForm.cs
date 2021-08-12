using System;
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
                txtAPIKey.Text = _configFile.ApiKey;
            }
            catch (Exception ex)
            {
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
                    _configFile.Save();
                    _configFile.ApiKey = apiKey.ToString();
                }
                else
                {
                    MessageBox.Show(@"Please enter valid Api Key.");

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
