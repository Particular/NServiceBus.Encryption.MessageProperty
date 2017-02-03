//namespace NServiceBus.Encryption.MessageProperty
//{
//    using System;
//    using System.Collections.Generic;
//    using Configuration.AdvanceExtensibility;
//    using Settings;
//
//    /// <summary>
//    /// Contains extension methods to NServiceBus.Configure.
//    /// </summary>
//    public static class ConfigureRijndaelEncryptionService
//    {
//        /// <summary>
//        /// Use 256 bit AES encryption based on the Rijndael cipher.
//        /// </summary>
//        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
//        /// <param name="encryptionKeyIdentifier">Encryption key identifier.</param>
//        /// <param name="encryptionKey">Encryption Key.</param>
//        /// <param name="decryptionKeys">A list of decryption keys.</param>
//        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IList<byte[]> decryptionKeys = null)
//        {
//            Guard.AgainstNull(nameof(config), config);
//            Guard.AgainstNullAndEmpty(nameof(encryptionKey), encryptionKey);
//
//            decryptionKeys = decryptionKeys ?? new List<byte[]>();
//
//            RegisterEncryptionService(config, () => BuildRijndaelEncryptionService(
//                encryptionKeyIdentifier,
//                new Dictionary<string, byte[]>
//                {
//                    {encryptionKeyIdentifier, encryptionKey}
//                },
//                decryptionKeys));
//        }
//
//        /// <summary>
//        /// Use 256 bit AES encryption based on the Rijndael cipher.
//        /// </summary>
//        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, IDictionary<string, byte[]> keys, IList<byte[]> decryptionKeys = null)
//        {
//            Guard.AgainstNull(nameof(config), config);
//            Guard.AgainstNull(nameof(encryptionKeyIdentifier), encryptionKeyIdentifier);
//            Guard.AgainstNull(nameof(keys), keys);
//
//            decryptionKeys = decryptionKeys ?? new List<byte[]>();
//
//            RegisterEncryptionService(config, () => BuildRijndaelEncryptionService(
//                encryptionKeyIdentifier,
//                keys,
//                decryptionKeys));
//        }
//
//        static IEncryptionService BuildRijndaelEncryptionService(
//            string encryptionKeyIdentifier,
//            IDictionary<string, byte[]> keys,
//            IList<byte[]> expiredKeys
//            )
//        {
//            return new RijndaelEncryptionService(
//                encryptionKeyIdentifier,
//                keys,
//                expiredKeys
//                );
//        }
//
//        /// <summary>
//        /// Register a custom <see cref="IEncryptionService" /> to be used for message encryption.
//        /// </summary>
//        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
//        /// <param name="func">
//        /// A delegate that constructs the instance of <see cref="IEncryptionService" /> to use for all
//        /// encryption.
//        /// </param>
//        public static void RegisterEncryptionService(this EndpointConfiguration config, Func<IEncryptionService> func)
//        {
//            Guard.AgainstNull(nameof(config), config);
//
//            config.GetSettings().Set(EncryptedServiceConstructorKey, func);
//        }
//
//        internal static Func<IEncryptionService> GetEncryptionServiceConstructor(this ReadOnlySettings settings)
//        {
//            return settings.Get<Func<IEncryptionService>>(EncryptedServiceConstructorKey);
//        }
//
//        internal const string EncryptedServiceConstructorKey = "MessagePropertyEncryptionServiceConstructor";
//    }
//}
