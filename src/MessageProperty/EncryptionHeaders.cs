namespace NServiceBus.Encryption.MessageProperty
{
    /// <summary>
    /// Headers used for encryption.
    /// </summary>
    public static class EncryptionHeaders
    {
        /// <summary>
        /// The identifier to lookup the key to decrypt the encrypted data.
        /// </summary>
        public const string RijndaelKeyIdentifier = "NServiceBus.RijndaelKeyIdentifier";
        /// <summary>
        /// The AES identifier to lookup the key to decrypt the encrypted data.
        /// </summary>
        public const string AesKeyIdentifier = "NServiceBus.AesKeyIdentifier";
    }
}