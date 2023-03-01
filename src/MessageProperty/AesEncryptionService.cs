//https://github.com/hibernating-rhinos/rhino-esb/blob/master/license.txt
//Copyright (c) 2005 - 2009 Ayende Rahien (ayende@ayende.com)
//All rights reserved.

//Redistribution and use in source and binary forms, with or without modification,
//are permitted provided that the following conditions are met:

//    * Redistributions of source code must retain the above copyright notice,
//    this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
//    * Neither the name of Ayende Rahien nor the names of its
//    contributors may be used to endorse or promote products derived from this
//    software without specific prior written permission.

//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
//THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Logging;
    using Pipeline;

    /// <summary>
    /// An <see cref="IEncryptionService"/> implementation usable for message property encryption using the Aes algorithm.
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        /// <summary>
        /// Creates a new instance of <see cref="AesEncryptionService" />
        /// </summary>
        /// <param name="encryptionKeyIdentifier">An identifier for the encryption key to be used to encrypt values.</param>
        /// <param name="key">The encryption key to be used for encryption and decryption.</param>
        public AesEncryptionService(
            string encryptionKeyIdentifier,
            byte[] key) : this(encryptionKeyIdentifier, new Dictionary<string, byte[]>
        {
            {
                encryptionKeyIdentifier, key
            }
        }, new List<byte[]>(0))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="AesEncryptionService"/>
        /// </summary>
        /// <param name="encryptionKeyIdentifier">An identifier for the encryption key to be used to encrypt values.</param>
        /// <param name="keys">A dictionary of available encryption keys and their identifiers for encryption and decryption.</param>
        public AesEncryptionService(
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys) : this(encryptionKeyIdentifier, keys, new List<byte[]>(0))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="AesEncryptionService"/>
        /// </summary>
        /// <param name="encryptionKeyIdentifier">An identifier for the encryption key to be used to encrypt values.</param>
        /// <param name="keys">A dictionary of available encryption keys and their identifiers for encryption and decryption.</param>
        /// <param name="decryptionKeys">A list of outdated encryption keys without identifiers which can be used for decryption.</param>
        public AesEncryptionService(
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys,
            IList<byte[]> decryptionKeys)
        {
            Guard.AgainstNullAndEmpty(nameof(encryptionKeyIdentifier), encryptionKeyIdentifier);
            Guard.AgainstNull(nameof(keys), keys);
            Guard.AgainstNull(nameof(decryptionKeys), decryptionKeys);

            this.encryptionKeyIdentifier = encryptionKeyIdentifier;
            this.decryptionKeys = decryptionKeys;
            this.keys = keys;

            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                Log.Error("No encryption key identifier configured. Messages with encrypted properties will fail to send. Add an encryption key identifier to the Aes encryption service configuration.");
            }
            else if (!keys.TryGetValue(encryptionKeyIdentifier, out encryptionKey))
            {
                throw new ArgumentException("No encryption key for given encryption key identifier.", nameof(encryptionKeyIdentifier));
            }
            else
            {
                VerifyEncryptionKey(encryptionKey);
            }

            VerifyExpiredKeys(decryptionKeys);
        }

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        public string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context)
        {

            if (TryGetKeyIdentifierHeader(out string keyIdentifier, context))
            {
                return DecryptUsingKeyIdentifier(encryptedValue, keyIdentifier);
            }
            Log.Warn($"Encrypted message has no '{EncryptionHeaders.AesKeyIdentifier}' header. Possibility of data corruption. Upgrade endpoints that send message with encrypted properties.");
            return DecryptUsingAllKeys(encryptedValue);
        }

        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        public EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context)
        {
            if (string.IsNullOrEmpty(encryptionKeyIdentifier))
            {
                throw new InvalidOperationException("The AES key identifier must be set.");
            }

            AddKeyIdentifierHeader(context);

            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.CBC;
                ConfigureIV(aes);

                using (var encryptor = aes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(value);
                    writer.Flush();
                    cryptoStream.Flush();
                    cryptoStream.FlushFinalBlock();
                    return new EncryptedValue
                    {
                        EncryptedBase64Value = Convert.ToBase64String(memoryStream.ToArray()),
                        Base64Iv = Convert.ToBase64String(aes.IV)
                    };
                }
            }
        }

        string DecryptUsingKeyIdentifier(EncryptedValue encryptedValue, string keyIdentifier)
        {

            if (!keys.TryGetValue(keyIdentifier, out byte[] decryptionKey))
            {
                throw new InvalidOperationException($"Decryption key not available for key identifier '{keyIdentifier}'. Add this key to the AES encryption service configuration. Key identifiers are case sensitive.");
            }

            try
            {
                return Decrypt(encryptedValue, decryptionKey);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Unable to decrypt property using configured decryption key specified in key identifier header.", ex);
            }
        }

        string DecryptUsingAllKeys(EncryptedValue encryptedValue)
        {
            var cryptographicExceptions = new List<CryptographicException>();

            foreach (var key in decryptionKeys)
            {
                try
                {
                    return Decrypt(encryptedValue, key);
                }
                catch (CryptographicException exception)
                {
                    cryptographicExceptions.Add(exception);
                }
            }
            var message = $"Could not decrypt message. Tried {decryptionKeys.Count} keys.";
            throw new AggregateException(message, cryptographicExceptions);
        }

        static string Decrypt(EncryptedValue encryptedValue, byte[] key)
        {
            var iv = Convert.FromBase64String(encryptedValue.Base64Iv);
            if (iv.Length == 16)
            {
                using (var aes = Aes.Create())
                {
                    var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = key;
                    using (var decryptor = aes.CreateDecryptor())
                    using (var memoryStream = new MemoryStream(encrypted))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            else
            {
#pragma warning disable SYSLIB0022
                using (var rijndael = new RijndaelManaged())
#pragma warning restore SYSLIB0022
                {
                    var encrypted = Convert.FromBase64String(encryptedValue.EncryptedBase64Value);
                    rijndael.BlockSize = iv.Length * 8;
                    rijndael.IV = iv;
                    rijndael.Mode = CipherMode.CBC;
                    rijndael.Key = key;
                    using (var decryptor = rijndael.CreateDecryptor())
                    using (var memoryStream = new MemoryStream(encrypted))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        static void VerifyExpiredKeys(IList<byte[]> keys)
        {
            for (var index = 0; index < keys.Count; index++)
            {
                var key = keys[index];
                if (IsValidKey(key))
                {
                    continue;
                }
                var message = $"The expired key at index {index} has an invalid length of {key.Length} bytes.";
                throw new Exception(message);
            }
        }

        static void VerifyEncryptionKey(byte[] key)
        {
            if (IsValidKey(key))
            {
                return;
            }
            var message = $"The encryption key has an invalid length of {key.Length} bytes.";
            throw new Exception(message);
        }

        static bool IsValidKey(byte[] key)
        {
            using (var aes = Aes.Create())
            {
                var bitLength = key.Length * 8;

                var maxValidKeyBitLength = aes.LegalKeySizes.Max(keyLength => keyLength.MaxSize);
                if (bitLength < maxValidKeyBitLength)
                {
                    Log.WarnFormat("Encryption key is {0} bits which is less than the maximum allowed {1} bits. Consider using a {2}-bit encryption key to obtain the maximum cipher strength", bitLength, maxValidKeyBitLength, maxValidKeyBitLength);
                }

                return aes.ValidKeySize(bitLength);
            }
        }

        /// <summary>
        /// Adds the key identifier of the currently used encryption key to the outgoing message's headers.
        /// </summary>
        protected internal virtual void AddKeyIdentifierHeader(IOutgoingLogicalMessageContext context)
        {
            context.Headers[EncryptionHeaders.AesKeyIdentifier] = encryptionKeyIdentifier;
            context.Headers[EncryptionHeaders.RijndaelKeyIdentifier] = encryptionKeyIdentifier;
        }

        /// <summary>
        /// Tries to locate an encryption key identifier from an incoming message.
        /// </summary>
        protected internal virtual bool TryGetKeyIdentifierHeader(out string keyIdentifier, IIncomingLogicalMessageContext context)
        {
            return context.Headers.TryGetValue(EncryptionHeaders.AesKeyIdentifier, out keyIdentifier);
        }

        /// <summary>
        /// Configures the initialization vector.
        /// </summary>
        protected internal virtual void ConfigureIV(Aes aes)
        {
            aes.GenerateIV();
        }

        readonly string encryptionKeyIdentifier;
        IList<byte[]> decryptionKeys; // Required, as we decrypt in the configured order.
        byte[] encryptionKey;
        IDictionary<string, byte[]> keys;
        static readonly ILog Log = LogManager.GetLogger<AesEncryptionService>();
    }
}