using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                string apiKey = Config.getApiKey();
                if (string.IsNullOrWhiteSpace(apiKey) == false)
                {
                    txtAPIKey.Text = apiKey;
                }
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
                string apiKey = txtAPIKey.Text.Trim();
                if (string.IsNullOrWhiteSpace(apiKey) == false)
                {
                    Config.setApiKey(apiKey);
                    Main.apiKey = apiKey;
                }
                else
                {
                    MessageBox.Show("Please enter valid Api Key.");
                    this.DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
