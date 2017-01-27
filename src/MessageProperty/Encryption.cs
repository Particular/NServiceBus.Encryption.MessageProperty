namespace NServiceBus.Encryption.MessageProperty
{
    using Features;

    class Encryption : Feature
    {
        public Encryption()
        {
            Prerequisite(c => c.Settings.HasSetting(ConfigureRijndaelEncryptionService.EncryptedServiceConstructorKey), "Encryption service not defined.");

            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var serviceConstructor = context.Settings.GetEncryptionServiceConstructor();
            var service = serviceConstructor();
            var inspector = new EncryptionInspector(context.Settings.Get<Conventions>());

            context.Pipeline.Register(new EncryptBehavior.EncryptRegistration(inspector, service));
            context.Pipeline.Register(new DecryptBehavior.DecryptRegistration(inspector, service));
        }
    }
}