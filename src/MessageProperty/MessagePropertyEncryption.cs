namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using Features;

    class MessagePropertyEncryption : Feature
    {
        public MessagePropertyEncryption()
        {
            Defaults(s => s.SetDefault<IsEncryptedPropertyConvention>(
                new IsEncryptedPropertyConvention(p =>
                typeof(EncryptedString).IsAssignableFrom(p.PropertyType))));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // check if the encryption service in the core has been enabled.
            // this check can be removed when encryption has been fully obsoleted in the core.
            if (context.Settings.HasSetting("EncryptionServiceConstructor"))
            {
                throw new Exception("Both NServiceBus.Encryption.MessageProperty.MessagePropertyEncryption and NServiceBus.Core's encryption feature are enabled. Disable one of the encryption features to avoid message payload corruption.");
            }

            var encryptionService = context.Settings.GetEncryptionService();
            var inspector = new EncryptionInspector(context.Settings.Get<IsEncryptedPropertyConvention>());

            context.Pipeline.Register(new EncryptBehavior.EncryptRegistration(inspector, encryptionService));
            context.Pipeline.Register(new DecryptBehavior.DecryptRegistration(inspector, encryptionService));
        }
    }
}