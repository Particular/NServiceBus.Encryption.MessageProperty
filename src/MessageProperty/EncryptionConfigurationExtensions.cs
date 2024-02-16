namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Reflection;
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Provides configuration extensions to configure message property encryption.
    /// </summary>
    public static class EncryptionConfigurationExtensions
    {
        /// <summary>
        /// Enable message property encryption using the given encryption service.
        /// </summary>
        /// <param name="configuration">The endpoint configurartion to extend.</param>
        /// <param name="encryptionService">The encryption service used to encrypt and decrypt message properties.</param>
        public static void EnableMessagePropertyEncryption(this EndpointConfiguration configuration, IEncryptionService encryptionService)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            ArgumentNullException.ThrowIfNull(encryptionService, nameof(encryptionService));

            configuration.GetSettings().Set(EncryptionServiceConfigurationKey, encryptionService);
            configuration.EnableFeature<MessagePropertyEncryption>();
        }

        /// <summary>
        /// Enable message property encryption using the given encryption service.
        /// </summary>
        /// <param name="configuration">The endpoint configurartion to extend.</param>
        /// <param name="encryptionService">The encryption service used to encrypt and decrypt message properties.</param>
        /// <param name="encryptedPropertyConvention">The convention which defines which properties should be encrypted. By default, all properties of type <see cref="EncryptedString"/> will be encrypted.</param>
        public static void EnableMessagePropertyEncryption(this EndpointConfiguration configuration, IEncryptionService encryptionService, Func<PropertyInfo, bool> encryptedPropertyConvention)
        {
            ArgumentNullException.ThrowIfNull(encryptedPropertyConvention, nameof(encryptedPropertyConvention));

            configuration.EnableMessagePropertyEncryption(encryptionService);
            configuration.GetSettings().Set(new IsEncryptedPropertyConvention(encryptedPropertyConvention));
        }

        internal static IEncryptionService GetEncryptionService(this IReadOnlySettings settings) => settings.Get<IEncryptionService>(EncryptionServiceConfigurationKey);

        const string EncryptionServiceConfigurationKey = "NServiceBus.Encryption.MessageProperty.EncryptionService";
    }
}