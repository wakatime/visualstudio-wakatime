using System;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class ApiKeyForm : Form
    {
        private readonly Shared.ExtensionUtils.WakaTime _wakaTime;

        public ApiKeyForm(ref Shared.ExtensionUtils.WakaTime wakaTime)
        {
            _wakaTime = wakaTime;
            InitializeComponent();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _wakaTime.Config.ApiKey;
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
                    _wakaTime.Config.ApiKey = apiKey.ToString();
                    _wakaTime.Config.Save();
                    _wakaTime.Config.ApiKey = apiKey.ToString();
                }
                else
                {
                    MessageBox.Show(@"Please enter valid Api Key.");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
