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
                UpdateAlgorithm();
                UpdateUsingTime();
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

        private void UpdateAlgorithm()
        {
            var temp = otp;
            if (temp != null)
            {
                temp.Algorithm = algorithmSelected;
            }
        }

        private bool usingTime = true;
        private int timeSelected = 30;
        private long counter = 0;

        private int settingNumber = 0;
        private bool UpdateUsingTime()
        {
            var valueUpdated = false;
            var temp = otp;
            if (temp != null)
            {
                var existingUsingTime = temp.TimeStep != 0;

                if (existingUsingTime != usingTime)
                {
                    if (usingTime)
                    {
                        if (temp.TimeStep != timeSelected)
                        {
                            temp.TimeStep = timeSelected;
                            counter = temp.Counter;
                            valueUpdated = true;
                        }
                    }
                    else
                    {
                        temp.TimeStep = 0;
                        temp.Counter = counter;
                        valueUpdated = true;
                    }
                }
                else
                {
                    if (usingTime)
                    {
                        if (temp.TimeStep != timeSelected)
                        {
                            temp.TimeStep = timeSelected;
                            valueUpdated = true;
                        }
                    }
                    else
                    {
                        if (temp.Counter != counter)
                        {
                            temp.Counter = counter;
                            valueUpdated = true;
                        }
                    }
                }
            }

            return valueUpdated;
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

            var code = temp.GetFormattedCode();

            lock (this)
            {
                try
                {
                    settingNumber ++;
                    counter = temp.Counter;
                    numericUpDown4.Value = counter;
                }
                finally
                {
                    settingNumber --;
                }
            }

            return code;

        }


        private void tmrUpdate_Tick(object sender, System.EventArgs e) {
            if (checkBox2.Checked)
            {
                SetCodeFromCurrent();
            }
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

        OneTimePasswordAlgorithm algorithmSelected = OneTimePasswordAlgorithm.Sha1;

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                algorithmSelected = OneTimePasswordAlgorithm.Sha512;
            }
            else if (radioButton2.Checked)
            {
                algorithmSelected = OneTimePasswordAlgorithm.Sha256;
            }
            else
            {
                algorithmSelected = OneTimePasswordAlgorithm.Sha1;
            }

            UpdateAlgorithm();
            SetCodeFromCurrent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            usingTime = checkBox1.Checked;
            UpdateUsingTimeWithRefreshIfRequired();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            timeSelected = (int) numericUpDown3.Value;
            UpdateUsingTimeWithRefreshIfRequired();
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            if (usingTime)
            {
                return;
            }
            var numberUpdated = false;
            if (settingNumber != 0)
            {
                return;
            }
            lock (this)
            {
                if (settingNumber != 0)
                {
                    return;
                }
                if (counter != (long) numericUpDown4.Value)
                {
                    settingNumber++;
                    numberUpdated = true;
                    counter = (long) numericUpDown4.Value;
                    settingNumber--;
                }
            }

            if (numberUpdated)
            {
                UpdateUsingTimeWithRefreshIfRequired();
            }
        }

        private void UpdateUsingTimeWithRefreshIfRequired()
        {
            if (UpdateUsingTime())
            {
                SetCodeFromCurrent();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SetCodeFromCurrent();
        }
    }
}
