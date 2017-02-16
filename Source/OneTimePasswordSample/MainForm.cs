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


        private void txtSecret_TextChanged(object sender, System.EventArgs e) {
            try {
                otp = new OneTimePassword(txtSecret.Text);
            } catch (ArgumentException) {
                otp = null;
            }
        }


        private void tmrUpdate_Tick(object sender, System.EventArgs e) {
            if (otp != null) {
                txtCode.Text = otp.GetCode().ToString("000000");
            } else {
                txtCode.Text = "Fix key!";
            }
        }

    }
}
