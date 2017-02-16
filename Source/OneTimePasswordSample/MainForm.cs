using Medo.Security.Cryptography;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace OneTimePasswordSample {
    internal partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            this.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2F);
        }

        private OneTimePassword otp;

        private void Form_Load(object sender, System.EventArgs e) {
            otp = new OneTimePassword(txtSecret.Text);
            tmrUpdate_Tick(null, null);
        }


        private void txtSecret_TextChanged(object sender, System.EventArgs e)
        {
            var textKey = txtSecret.Text;
            SetNewKeyFromText(textKey);
        }

        private void SetNewKeyFromText(string textKey)
        {
            try
            {
                otp = new OneTimePassword(textKey);
                SetDigits();
                SetCodeFromCurrent();
            }
            catch (ArgumentException)
            {
                otp = null;
            }
        }

        private void SetDigits(int value)
        {
            var temp = otp;
            if (temp != null)
            {
                temp.Digits = value;
            }
        }

        private void SetCodeFromCurrent()
        {
            txtCode.Text = GetCode();
        }

        private string GetCode()
        {
            var temp = otp;
            if (temp == null)
            {
                return "Fix key!";
            }

            return temp.GetFormattedCode();
        }


        private void tmrUpdate_Tick(object sender, System.EventArgs e) {
            SetCodeFromCurrent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtSecret.Text = new SecretKey((int)numericUpDown1.Value).GetBase32Secret();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            SetDigits();
            SetCodeFromCurrent();
        }

        private void SetDigits()
        {
            SetDigits((int) numericUpDown2.Value);
        }
    }
}
