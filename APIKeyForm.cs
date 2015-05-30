using System;
using System.Windows.Forms;

namespace WakaTime
{
    public partial class ApiKeyForm : Form
    {
        public ApiKeyForm()
        {
            InitializeComponent();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = Config.GetApiKey().Trim();
                txtProxy.Text = Config.GetProxy().Trim();
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
                Guid apiKey;
                var parse = Guid.TryParse(txtAPIKey.Text.Trim(), out apiKey);                              
                if (parse)
                {
                    Config.SetApiKey(apiKey.ToString());
                    Config.SetProxy(string.IsNullOrEmpty(txtProxy.Text.Trim()) ? null : txtProxy.Text);
                    WakaTimePackage.ApiKey = apiKey.ToString();
                }
                else
                {
                    MessageBox.Show("Please enter valid Api Key.");
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
