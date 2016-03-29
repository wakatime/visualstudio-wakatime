using System;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class ApiKeyForm : Form
    {
        private readonly WakaTimeConfigFile _wakaTimeConfigFile;
        private static Timer timer;

        public ApiKeyForm()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(TimerEventProcessor);

            InitializeComponent();

            _wakaTimeConfigFile = new WakaTimeConfigFile();
        }

        private void ApiKeyForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _wakaTimeConfigFile.ApiKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Make sure form is focused
            timer.Start();
        }

        private void TimerEventProcessor(object sender, EventArgs e)
        {
            this.Focus();
            if (this.Focused)
            {
                timer.Stop();
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
                    _wakaTimeConfigFile.Save();
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
