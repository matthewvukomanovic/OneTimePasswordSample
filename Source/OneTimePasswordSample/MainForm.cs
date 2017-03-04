using Medo.Security.Cryptography;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace OneTimePasswordSample {
    internal partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2F);
        }

        private OneTimePassword _otp;

        private void Form_Load(object sender, EventArgs e) {
            _otp = new OneTimePassword(txtSecret.Text);
            tmrUpdate_Tick(null, null);
        }


        private void txtSecret_TextChanged(object sender, EventArgs e)
        {
            var textKey = txtSecret.Text;
            SetNewKeyFromText(textKey);
        }

        private void SetNewKeyFromText(string textKey)
        {
            try
            {
                _otp = new OneTimePassword(textKey);
                SetDigits();
                UpdateAlgorithm();
                UpdateUsingTime();
                SetCodeFromCurrent();
            }
            catch (ArgumentException)
            {
                _otp = null;
            }
        }

        private void SetDigits(int value)
        {
            var temp = _otp;
            if (temp != null)
            {
                temp.Digits = value;
            }
        }

        private void UpdateAlgorithm()
        {
            var temp = _otp;
            if (temp != null)
            {
                temp.Algorithm = _algorithmSelected;
            }
        }

        private bool _usingTime = true;
        private int _timeSelected = 30;
        private long _counter = 0;

        private int _settingNumber = 0;
        private bool UpdateUsingTime()
        {
            var valueUpdated = false;
            var temp = _otp;
            if (temp != null)
            {
                var existingUsingTime = temp.TimeStep != 0;

                if (existingUsingTime != _usingTime)
                {
                    if (_usingTime)
                    {
                        if (temp.TimeStep != _timeSelected)
                        {
                            temp.TimeStep = _timeSelected;
                            _counter = temp.Counter;
                            valueUpdated = true;
                        }
                    }
                    else
                    {
                        temp.TimeStep = 0;
                        temp.Counter = _counter;
                        valueUpdated = true;
                    }
                }
                else
                {
                    if (_usingTime)
                    {
                        if (temp.TimeStep != _timeSelected)
                        {
                            temp.TimeStep = _timeSelected;
                            valueUpdated = true;
                        }
                    }
                    else
                    {
                        if (temp.Counter != _counter)
                        {
                            temp.Counter = _counter;
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
            var temp = _otp;
            if (temp == null)
            {
                return "Fix key!";
            }

            var code = temp.GetFormattedCode();

            lock (this)
            {
                try
                {
                    _settingNumber ++;
                    _counter = temp.Counter;
                    numericUpDown4.Value = _counter;
                }
                finally
                {
                    _settingNumber --;
                }
            }

            return code;

        }


        private void tmrUpdate_Tick(object sender, EventArgs e) {
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

        OneTimePasswordAlgorithm _algorithmSelected = OneTimePasswordAlgorithm.Sha1;

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                _algorithmSelected = OneTimePasswordAlgorithm.Sha512;
            }
            else if (radioButton2.Checked)
            {
                _algorithmSelected = OneTimePasswordAlgorithm.Sha256;
            }
            else
            {
                _algorithmSelected = OneTimePasswordAlgorithm.Sha1;
            }

            UpdateAlgorithm();
            SetCodeFromCurrent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _usingTime = checkBox1.Checked;
            UpdateUsingTimeWithRefreshIfRequired();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            _timeSelected = (int) numericUpDown3.Value;
            UpdateUsingTimeWithRefreshIfRequired();
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            if (_usingTime)
            {
                return;
            }
            var numberUpdated = false;
            if (_settingNumber != 0)
            {
                return;
            }
            lock (this)
            {
                if (_settingNumber != 0)
                {
                    return;
                }
                if (_counter != (long) numericUpDown4.Value)
                {
                    _settingNumber++;
                    numberUpdated = true;
                    _counter = (long) numericUpDown4.Value;
                    _settingNumber--;
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
