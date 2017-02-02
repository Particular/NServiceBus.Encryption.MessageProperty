namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using Features;

    class Encryption : Feature
    {
        public Encryption()
        {
            Prerequisite(c => c.Settings.HasSetting(ConfigureRijndaelEncryptionService.EncryptedServiceConstructorKey), "Encryption service not defined.");
            Defaults(s => s.SetDefault<Conventions>(new Conventions()));
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // check if the encryption service in the core has been enabled.
            // this check can be removed when encryption has been fully obsoleted in the core.
            if (context.Settings.HasSetting("EncryptionServiceConstructor"))
            {
                throw new Exception("The message property encryption extension as well as NServiceBus.Core's encryption feature are enabled. Disable one of the encryption features to avoid message payload corruption.");
            }

            var serviceConstructor = context.Settings.GetEncryptionServiceConstructor();
            var service = serviceConstructor();
            var inspector = new EncryptionInspector(context.Settings.Get<Conventions>());

            context.Pipeline.Register(new EncryptBehavior.EncryptRegistration(inspector, service));
            context.Pipeline.Register(new DecryptBehavior.DecryptRegistration(inspector, service));
        }
    }
}