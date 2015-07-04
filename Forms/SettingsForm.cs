using System;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly WakaTimeConfigFile _wakaTimeConfigFile;

        public SettingsForm()
        {
            InitializeComponent();

            _wakaTimeConfigFile = new WakaTimeConfigFile();            
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _wakaTimeConfigFile.ApiKey;
                txtProxy.Text = _wakaTimeConfigFile.Proxy;
                cboWorkplace.SelectedItem = _wakaTimeConfigFile.WorkPlace;
                chkDebugMode.Checked = _wakaTimeConfigFile.Debug;
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
                    _wakaTimeConfigFile.ApiKey = apiKey.ToString();
                    _wakaTimeConfigFile.Proxy = txtProxy.Text.Trim();
                    _wakaTimeConfigFile.WorkPlace = cboWorkplace.SelectedItem.ToString();
                    _wakaTimeConfigFile.Debug = chkDebugMode.Checked;
                    _wakaTimeConfigFile.Save();
                    WakaTimePackage.ApiKey = apiKey.ToString();
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
