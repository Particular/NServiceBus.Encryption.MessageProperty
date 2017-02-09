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

        public static void EncryptValue(this IEncryptionService encryptionService, WireEncryptedString wireEncryptedString, IOutgoingLogicalMessageContext context)
        {
            var ev = encryptionService.Encrypt(wireEncryptedString.Value, context);
            wireEncryptedString.EncryptedValue = new NServiceBus.EncryptedValue
            {
                EncryptedBase64Value = ev.EncryptedBase64Value,
                Base64Iv = ev.Base64Iv
            };
            wireEncryptedString.Value = null;
        }

        public static void DecryptValue(this IEncryptionService encryptionService, EncryptedString encryptedString, IIncomingLogicalMessageContext context)
        {
            if (encryptedString.EncryptedValue == null)
            {
                throw new Exception("Encrypted property is missing encryption data");
            }

            encryptedString.Value = encryptionService.Decrypt(encryptedString.EncryptedValue, context);
        }

        public static void DecryptValue(this IEncryptionService encryptionService, WireEncryptedString wireEncryptedString, IIncomingLogicalMessageContext context)
        {
            if (wireEncryptedString.EncryptedValue == null)
            {
                throw new Exception("Encrypted property is missing encryption data");
            }

            var encryptedValue = new EncryptedValue
            {
                Base64Iv = wireEncryptedString.EncryptedValue.Base64Iv,
                EncryptedBase64Value = wireEncryptedString.EncryptedValue.EncryptedBase64Value
            };
            wireEncryptedString.Value = encryptionService.Decrypt(encryptedValue, context);
        }
    }
}