using Medo.Security.Cryptography;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OneTimePasswordSample {
    internal partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2F);
        }

        private OneTimePassword _otp;
        private OneTimePassword _otpCached;

        private void Form_Load(object sender, EventArgs e)
        {
            SetNewKeyFromText(txtSecret.Text);
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
                _otpCached = new OneTimePassword(textKey);

                SetDigits();
                UpdateAlgorithm();
                UpdateUsingTime();
                UpdateTolerance();
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
        private int _previousTolerance = 1;
        private int _futureTolerance = 0;

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


        private bool UpdateTolerance()
        {
            var valueUpdated = false;
            var temp = _otp;
            if (temp != null)
            {
                if (temp.ToleranceNext != _futureTolerance)
                {
                    temp.ToleranceNext = _futureTolerance;
                    valueUpdated = true;
                }
                if (temp.TolerancePrev != _previousTolerance)
                {
                    temp.TolerancePrev = _previousTolerance;
                    valueUpdated = true;
                }
            }

            return valueUpdated;
        }


        private void SetCodeFromCurrent()
        {
            var valid = false;
            var temp = _otp;
            var temp2 = _otpCached;
            if (temp != null)
            {
                temp2.CopySettingsFrom(_otp);
                var counter = _otp.Counter;
                temp2.TimeStep = 0;
                temp2.Counter = counter;
                valid = true;
            }

            txtCode.Text = GetCode();
            TimeSpan timeleft = TimeSpan.Zero;
            if (valid && temp.TimeStep != 0)
            {
                timeleft = temp.TimeLeft;
                textBox4.Text = timeleft.ToString("%s");
            }
            else
            {
                textBox4.Text = "";
            }

            var valueToSet = string.Empty;
            if (valid)
            {
                var current = temp2.Counter;
                var from = temp2.Counter - temp2.TolerancePrev;
                var to = temp2.Counter + temp2.ToleranceNext;

                StringBuilder sb = new StringBuilder();
                var seenBefore = false;
                var seenAfter = false;
                for (var i = from; i <= to; i++)
                {
                    temp2.Counter = i;

                    if (i < current && !seenBefore)
                    {
                        sb.AppendLine("Previous:");
                        seenBefore = true;
                    }

                    if (i > current && !seenAfter)
                    {
                        sb.AppendLine("\r\nFuture:");
                        seenAfter = true;
                    }

                    if (i == current)
                    {
                        sb.AppendLine("\r\nCurrent:" + (temp.TimeStep != 0 ? " " + timeleft.ToString("%s") : ""));
                    }

                    sb.AppendLine(temp2.GetFormattedCode());
                }

                valueToSet = sb.ToString();
            }

            textBox1.Text = valueToSet;
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

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            _previousTolerance = (int) numericUpDown5.Value;
            UpdateTolerance();
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            _futureTolerance = (int)numericUpDown6.Value;
            UpdateTolerance();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_otp != null)
            {
                var verifyCode = textBox2.Text;
                var valid = _otp.IsCodeValid(verifyCode);
                textBox3.Text = valid ? "Is Valid" : "Invalid";

                if (valid && _otp.TimeStep == 0)
                {
                    _otp.Counter --;
                    SetCodeFromCurrent();
                }
            }
        }
    }
}
