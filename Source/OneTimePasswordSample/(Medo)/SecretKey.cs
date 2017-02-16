//Copyright (c) 2015 Josip Medved <jmedved@jmedved.com>

//2015-02-12: Initial version.


using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Medo.Security.Cryptography
{

    /// <summary>
    /// Implementation of HOTP (RFC 4226) and TOTP (RFC 6238) one-time password algorithms.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "One time is more commonly written as two words.")]
    public class SecretKey
    {

        /// <summary>
        /// Create new instance with random 160-bit secret.
        /// </summary>
        public SecretKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(this.SecretBuffer);
            }
            this.SecretLength = 20; //160 bits
            ProtectSecret();
        }

        /// <summary>
        /// Create new instance with predefined secret.
        /// </summary>
        /// <param name="secret">Secret. It should not be shorter than 128 bits (16 bytes). Minimum of 160 bits (20 bytes) is strongly recommended.</param>
        /// <exception cref="System.ArgumentNullException">Secret cannot be null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Secret cannot be longer than 8192 bits (1024 bytes).</exception>
        public SecretKey(byte[] secret)
        {
            if (secret == null) { throw new ArgumentNullException("secret", "Secret cannot be null."); }
            if (secret.Length > this.SecretBuffer.Length) { throw new ArgumentOutOfRangeException("secret", "Secret cannot be longer than 8192 bits (1024 bytes)."); }

            Buffer.BlockCopy(secret, 0, this.SecretBuffer, 0, secret.Length);
            this.SecretLength = secret.Length;
            ProtectSecret();
        }

        /// <summary>
        /// Create new instance with predefined secret.
        /// </summary>
        /// <param name="secret">Secret in Base32 encoding. It should not be shorter than 128 bits (16 bytes). Minimum of 160 bits (20 bytes) is strongly recommended.</param>
        /// <exception cref="System.ArgumentNullException">Secret cannot be null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Secret is not valid Base32 string. -or- Secret cannot be longer than 8192 bits (1024 bytes).</exception>
        public SecretKey(string secret)
        {
            if (secret == null) { throw new ArgumentNullException("secret", "Secret cannot be null."); }

            try
            {
                int length;
                FromBase32(secret, this.SecretBuffer, out length);
                this.SecretLength = length;
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("secret", "Secret cannot be longer than 8192 bits (1024 bytes).");
            }
            catch (Exception)
            {
                throw new ArgumentOutOfRangeException("secret", "Secret is not valid Base32 string.");
            }
            ProtectSecret();
        }

        #region Secret buffer

        private readonly byte[] SecretBuffer = new byte[1024]; //ProtectedMemory requires length of the data to be a multiple of 16 bytes.
        private readonly int SecretLength;

        private void ProtectSecret()
        {
            ProtectedMemory.Protect(this.SecretBuffer, MemoryProtectionScope.SameProcess);
        }

        private void UnprotectSecret()
        {
            ProtectedMemory.Unprotect(this.SecretBuffer, MemoryProtectionScope.SameProcess);
        }


        /// <summary>
        /// Returns secret in byte array.
        /// It is up to the caller to secure given byte array.
        /// </summary>
        public byte[] GetSecret()
        {
            var buffer = new byte[this.SecretLength];

            this.UnprotectSecret();
            try
            {
                Buffer.BlockCopy(this.SecretBuffer, 0, buffer, 0, buffer.Length);
            }
            finally
            {
                this.ProtectSecret();
            }

            return buffer;
        }

        /// <summary>
        /// Returns secret as a Base32 string.
        /// String will be shown in quads and without padding.
        /// It is up to the caller to secure given string.
        /// </summary>
        public string GetBase32Secret()
        {
            return this.GetBase32Secret(SecretFormatFlags.Spacing);
        }

        /// <summary>
        /// Returns secret as a Base32 string with custom formatting.
        /// It is up to the caller to secure given string.
        /// </summary>
        /// <param name="format">Format of Base32 string.</param>
        public string GetBase32Secret(SecretFormatFlags format)
        {
            this.UnprotectSecret();
            try
            {
                return ToBase32(this.SecretBuffer, this.SecretLength, format);
            }
            finally
            {
                this.ProtectSecret();
            }
        }

        #endregion


        #region Base32

        private static readonly IList<char> Base32Alphabet = new List<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567").AsReadOnly();
        private static readonly byte[] Base32Bitmask = new byte[] { 0x00, 0x01, 0x03, 0x07, 0x0F, 0x1F };

        private static void FromBase32(string text, byte[] buffer, out int length)
        {
            var index = 0;

            var bitPosition = 0;
            byte partialByte = 0;
            foreach (var ch in text)
            { //always assume padding - easier to code than actually checking
                if (char.IsWhiteSpace(ch)) { continue; } //ignore whitespaces
                if (ch == '=')
                { // finish up
                    bitPosition = -1;
                    continue;
                }
                else if (bitPosition == -1) { throw new FormatException("Character '" + ch + "' found after padding ."); }

                var bits = Base32Alphabet.IndexOf(char.ToUpperInvariant(ch));
                if (bits < 0) { throw new FormatException("Unknown character '" + ch + "'."); }

                var bitCount1 = (bitPosition < 3) ? 5 : 8 - bitPosition; //how many bits go in current partial byte
                var bitCount2 = 5 - bitCount1; //how many bits are for next byte

                partialByte <<= bitCount1;
                partialByte |= (byte)(bits >> (5 - bitCount1));
                bitPosition += bitCount1;

                if (bitPosition >= 8)
                {
                    buffer[index] = partialByte;
                    index++;
                    bitPosition = bitCount2;
                    partialByte = (byte)(bits & Base32Bitmask[bitCount2]);
                }
            }

            if ((bitPosition > -1) && (bitPosition >= 5))
            {
                partialByte <<= (8 - bitPosition);
                buffer[index] = partialByte;
                index++;
            }

            length = index;
        }

        private static string ToBase32(byte[] bytes, int length, SecretFormatFlags format)
        {
            if (length == 0) { return string.Empty; }

            var hasSpacing = (format & SecretFormatFlags.Spacing) == SecretFormatFlags.Spacing;
            var hasPadding = (format & SecretFormatFlags.Padding) == SecretFormatFlags.Padding;
            var isUpper = (format & SecretFormatFlags.Uppercase) == SecretFormatFlags.Uppercase;

            var bitLength = (length * 8);
            var textLength = bitLength / 5 + ((bitLength % 5) == 0 ? 0 : 1);
            var totalLength = textLength;

            var padLength = (textLength % 8 == 0) ? 0 : 8 - textLength % 8;
            totalLength += (hasPadding ? padLength : 0);

            var spaceLength = totalLength / 4 + ((totalLength % 4 == 0) ? -1 : 0);
            totalLength += (hasSpacing ? spaceLength : 0);


            var chars = new char[totalLength];
            var index = 0;

            var bits = 0;
            var bitsRemaining = 0;
            for (int i = 0; i < length; i++)
            {
                bits = (bits << 8) | bytes[i];
                bitsRemaining += 8;
                while (bitsRemaining >= 5)
                {
                    var bitsIndex = (bits >> (bitsRemaining - 5)) & 0x1F;
                    bitsRemaining -= 5;
                    chars[index] = isUpper ? Base32Alphabet[bitsIndex] : char.ToLowerInvariant(Base32Alphabet[bitsIndex]);
                    index++;

                    if (hasSpacing && (index < chars.Length) && (bitsRemaining % 4 == 0))
                    {
                        chars[index] = ' ';
                        index++;
                    }
                }
            }
            if (bitsRemaining > 0)
            {
                var bitsIndex = (bits & Base32Bitmask[bitsRemaining]) << (5 - bitsRemaining);
                chars[index] = isUpper ? Base32Alphabet[bitsIndex] : char.ToLowerInvariant(Base32Alphabet[bitsIndex]);
                index++;
            }

            if (hasPadding)
            {
                for (int i = 0; i < padLength; i++)
                {
                    if (hasSpacing && (i % 4 == padLength % 4))
                    {
                        chars[index] = ' ';
                        index++;
                    }
                    chars[index] = '=';
                    index++;
                }
            }

            return new string(chars);
        }

        #endregion

    }

    /// <summary>
    /// Enumerates formatting option for secret.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "Identifier name is intentional.")]
    [Flags()]
    public enum SecretFormatFlags
    {
        /// <summary>
        /// Secret will be returned as a minimal Base32 string.
        /// </summary>
        None = 0,
        /// <summary>
        /// Secret will have space every four characters.
        /// </summary>
        Spacing = 1,
        /// <summary>
        /// Secret will be properly padded to full Base32 length.
        /// </summary>
        Padding = 2,
        /// <summary>
        /// Secret will be returned in upper case characters.
        /// </summary>
        Uppercase = 4,
    }

}
