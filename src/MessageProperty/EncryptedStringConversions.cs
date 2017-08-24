namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using Pipeline;

    static class EncryptedStringConversions
    {
        public static void EncryptValue(this IEncryptionService encryptionService, EncryptedString encryptedString, IOutgoingLogicalMessageContext context)
        {
            encryptedString.EncryptedValue = encryptionService.Encrypt(encryptedString.Value, context);
            encryptedString.Value = null;
        }

        public static void DecryptValue(this IEncryptionService encryptionService, EncryptedString encryptedString, IIncomingLogicalMessageContext context)
        {
            if (encryptedString.EncryptedValue == null)
            {
                throw new Exception("Encrypted property is missing encryption data");
            }

            encryptedString.Value = encryptionService.Decrypt(encryptedString.EncryptedValue, context);
        }
    }
}