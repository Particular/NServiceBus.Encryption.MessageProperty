namespace NServiceBus.Encryption.MessageProperty
{
    using Configuration.AdvanceExtensibility;
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
            configuration.GetSettings().Set(EncryptionServiceConfigurationKey, encryptionService);
            configuration.EnableFeature<Encryption>();
        }

        internal static IEncryptionService GetEncryptionService(this ReadOnlySettings settings)
        {
            return settings.Get<IEncryptionService>(EncryptionServiceConfigurationKey);
        }

        const string EncryptionServiceConfigurationKey = "NServiceBus.Encryption.MessageProperty.EncryptionService";
    }
}