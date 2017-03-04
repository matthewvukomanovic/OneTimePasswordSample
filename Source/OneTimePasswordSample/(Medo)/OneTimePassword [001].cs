//Copyright (c) 2015 Josip Medved <jmedved@jmedved.com>

//2015-02-12: Initial version.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Medo.Security.Cryptography {

    /// <summary>
    /// Implementation of HOTP (RFC 4226) and TOTP (RFC 6238) one-time password algorithms.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "One time is more commonly written as two words.")]
    public class OneTimePassword {

        private readonly SecretKey _secretKey;

        /// <summary>
        /// Create new instance with random 160-bit secret.
        /// </summary>
        public OneTimePassword() : this(new SecretKey()) {
        }

        private OneTimePassword(SecretKey secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }
            _secretKey = secret;
        }

        /// <summary>
        /// Create new instance with predefined secret.
        /// </summary>
        /// <param name="secret">Secret. It should not be shorter than 128 bits (16 bytes). Minimum of 160 bits (20 bytes) is strongly recommended.</param>
        /// <exception cref="System.ArgumentNullException">Secret cannot be null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Secret cannot be longer than 8192 bits (1024 bytes).</exception>
        public OneTimePassword(byte[] secret) : this(new SecretKey(secret)) {
        }

        /// <summary>
        /// Create new instance with predefined secret.
        /// </summary>
        /// <param name="secret">Secret in Base32 encoding. It should not be shorter than 128 bits (16 bytes). Minimum of 160 bits (20 bytes) is strongly recommended.</param>
        /// <exception cref="System.ArgumentNullException">Secret cannot be null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Secret is not valid Base32 string. -or- Secret cannot be longer than 8192 bits (1024 bytes).</exception>
        public OneTimePassword(string secret) : this ( new SecretKey(secret)) {
        }

        #region Setup

        private int _digits = 6;
        /// <summary>
        /// Gets/Sets number of digits to return.
        /// Number of digits should be kept between 6 and 8 for best results.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Number of digits to return must be between 4 and 9.</exception>
        public int Digits {
            get { return _digits; }
            set {
                if ((value < 4) || (value > 9)) { throw new ArgumentOutOfRangeException("value", "Number of digits to return must be between 4 and 9."); }
                _digits = value;
            }
        }

        private int _timeStep = 30;
        /// <summary>
        /// Gets/sets time step in seconds for TOTP algorithm.
        /// Value must be between 15 and 300 seconds.
        /// If value is zero, time step won't be used and HOTP will be resulting protocol.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Time step must be between 15 and 300 seconds.</exception>
        public int TimeStep {
            get { return _timeStep; }
            set {
                if (value == 0) {
                    _timeStep = 0;
                    Counter = 0;
                } else {
                    if ((value < 0) || (value > 86400)) { throw new ArgumentOutOfRangeException("value", "Time step must be between 0 and 86400 seconds."); }
                    _timeStep = value;
                }
            }
        }

        private readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private DateTime _testTime = DateTime.MinValue;

        private long _counter = 0;
        /// <summary>
        /// Gets/sets counter value.
        /// Value can only be set in HOTP mode (if time step is zero).
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Counter value must be a positive number.</exception>
        /// <exception cref="System.NotSupportedException">Counter value can only be set in HOTP mode (time step is zero).</exception>
        public long Counter {
            get {
                if (TimeStep == 0) {
                    return _counter;
                } else {
                    var currTime = (_testTime > DateTime.MinValue) ? _testTime : DateTime.UtcNow;
                    var seconds = (currTime.Ticks - _epoch.Ticks) / 10000000;
                    return (seconds / TimeStep);
                }
            }
            set {
                if (TimeStep == 0) {
                    if (value < 0) { throw new ArgumentOutOfRangeException("value", "Counter value must be a positive number."); }
                    _counter = value;
                } else {
                    throw new NotSupportedException("Counter value can only be set in HOTP mode (time step is zero).");
                }
            }
        }

        private OneTimePasswordAlgorithm _algorithm = OneTimePasswordAlgorithm.Sha1;
        /// <summary>
        /// Gets/sets crypto algorithm.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Unknown algorithm.</exception>
        public OneTimePasswordAlgorithm Algorithm {
            get { return _algorithm; }
            set {
                switch (value) {
                    case OneTimePasswordAlgorithm.Sha1:
                    case OneTimePasswordAlgorithm.Sha256:
                    case OneTimePasswordAlgorithm.Sha512: break;
                    default: throw new ArgumentOutOfRangeException("value", "Unknown algorithm.");
                }
                _algorithm = value;
            }
        }

        public SecretKey SecretKey
        {
            get { return _secretKey; }
        }

        #endregion

        #region Code

        /// <summary>
        /// Returns code.
        /// In HOTP mode (time step is zero), counter will be automatically increased. 
        /// </summary>
        public int GetCode() {
            return GetCode(Digits);
        }

        public string GetFormattedCode()
        {
            var digits = Digits;
            return GetFormattedCode(digits);
        }

        public string GetFormattedCode(int digits)
        {
            return GetCode(digits).ToString(GetFormatStringForDigits(digits));
        }

        private static string GetFormatStringForDigits(int digits)
        {
            string formatString;
            switch (digits)
            {
                case 4:
                    formatString = "00 00";
                    break;
                case 5:
                    formatString = "00 0 00";
                    break;
                case 6:
                    formatString = "000 000";
                    break;
                case 7:
                    formatString = "00 000 00";
                    break;
                case 8:
                    formatString = "000 00 000";
                    break;
                case 9:
                    formatString = "000 000 000";
                    break;
                default:
                    formatString = "";
                    break;
            }
            return formatString;
        }

        private int _cachedDigits;
        private long _cachedCounter = -1;
        private int _cachedCode;
        private OneTimePasswordAlgorithm? _cachedAlgorithm = null;


        /// <summary>
        /// Returns code.
        /// In HOTP mode (time step is zero), counter will be automatically increased. 
        /// Number of digits should be kept between 6 and 8 for best results.
        /// </summary>
        /// <param name="digits">Number of digits to return.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Number of digits to return must be between 4 and 9.</exception>
        public int GetCode(int digits) {
            if ((digits < 4) || (digits > 9)) { throw new ArgumentOutOfRangeException("digits", "Number of digits to return must be between 4 and 9."); }

            var counter = Counter;

            if ((_cachedCounter == counter) && (_cachedDigits == digits) && _cachedAlgorithm != null && _cachedAlgorithm.Value.Equals(Algorithm))
            {
                if (TimeStep == 0) { Counter = counter + 1; }
                return _cachedCode;
            } //to avoid recalculation if all is the same

            var code = GetCode(counter, digits);
            if (TimeStep == 0) { Counter = counter + 1; }

            _cachedDigits = digits;
            _cachedCounter = counter;
            _cachedCode = code;
            _cachedAlgorithm = Algorithm;

            return code;
        }

        private int GetCode(long counter, int digits) {
            byte[] hash;

            var secret = SecretKey.GetSecret();
            try {
                var counterBytes = BitConverter.GetBytes(counter);
                if (BitConverter.IsLittleEndian) { Array.Reverse(counterBytes, 0, 8); }
                HMAC hmac = null;
                try {
                    switch (Algorithm) {
                        case OneTimePasswordAlgorithm.Sha1: hmac = new HMACSHA1(secret); break;
                        case OneTimePasswordAlgorithm.Sha256: hmac = new HMACSHA256(secret); break;
                        case OneTimePasswordAlgorithm.Sha512: hmac = new HMACSHA512(secret); break;
                    }
                    Debug.Assert(hmac != null, "hmac != null");
                    hash = hmac.ComputeHash(counterBytes);
                } finally {
                    if (hmac != null) { hmac.Dispose(); }
                }
            } finally {
                Array.Clear(secret, 0, secret.Length);
            }

            int offset = hash[hash.Length - 1] & 0x0F;
            var truncatedHash = new byte[] { (byte)(hash[offset + 0] & 0x7F), hash[offset + 1], hash[offset + 2], hash[offset + 3] };
            if (BitConverter.IsLittleEndian) { Array.Reverse(truncatedHash, 0, 4); }
            var number = BitConverter.ToInt32(truncatedHash, 0);

            return number % DigitsDivisor[digits];
        }

        private static readonly int[] DigitsDivisor = new int[] { 0, 0, 0, 0, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };

        #endregion

        #region Validate

        /// <summary>
        /// Returns true if code has been validated.
        /// </summary>
        /// <param name="code">Code to validate.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Code must contain only numbers and whitespace.</exception>
        /// <exception cref="System.ArgumentNullException">Code cannot be null.</exception>
        public bool IsCodeValid(string code) {
            if (code == null) { throw new ArgumentNullException("code", "Code cannot be null."); }
            var number = 0;
            foreach (var ch in code) {
                if (char.IsWhiteSpace(ch)) { continue; }
                if (!char.IsDigit(ch)) { throw new ArgumentOutOfRangeException("code", "Code must contain only numbers and whitespace."); }
                if (number >= 100000000) { return false; } //number cannot be more than 9 digits
                number *= 10;
                number += (ch - 0x30);
            }
            return IsCodeValid(number);
        }

        /// <summary>
        /// Returns true if code has been validated.
        /// In HOTP mode (time step is zero) counter will increased if code is valid.
        /// </summary>
        /// <param name="code">Code to validate.</param>
        public bool IsCodeValid(int code) {
            var currCode = GetCode(Counter, Digits);
            var prevCode = GetCode(Counter - 1, Digits);

            var isCurrValid = (code == currCode);
            var isPrevValid = (code == prevCode) && (Counter > 0); //don't check previous code if counter is zero; but calculate it anyhow (to keep timing)
            var isValid = isCurrValid || isPrevValid;
            if ((TimeStep == 0) && isValid) {
                Counter++;
            }
            return isValid;
        }

        #endregion
    }

    /// <summary>
    /// Algorithm for generating one time password.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "One time is more commonly written as two words.")]
    public enum OneTimePasswordAlgorithm {
        /// <summary>
        /// SHA-1.
        /// </summary>
        Sha1 = 0,
        /// <summary>
        /// SHA-256.
        /// </summary>
        Sha256 = 1,
        /// <summary>
        /// SHA-512.
        /// </summary>
        Sha512 = 2,
    }

}
