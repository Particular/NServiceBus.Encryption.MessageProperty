namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    class EncryptBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public EncryptBehavior(EncryptionInspector messageInspector, IEncryptionService encryptionService)
        {
            this.messageInspector = messageInspector;
            this.encryptionService = encryptionService;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var currentMessageToSend = context.Message.Instance;

            foreach (var item in messageInspector.ScanObject(currentMessageToSend))
            {
                EncryptMember(item.Item1, item.Item2, context);
            }

            context.UpdateMessage(currentMessageToSend);

            return next(context);
        }

        void EncryptMember(object message, MemberInfo member, IOutgoingLogicalMessageContext context)
        {
            var valueToEncrypt = member.GetValue(message);

            var wireEncryptedString = valueToEncrypt as EncryptedString;
            if (wireEncryptedString != null)
            {
                encryptionService.EncryptValue(wireEncryptedString, context);
                return;
            }

            var stringToEncrypt = valueToEncrypt as string;
            if (stringToEncrypt != null)
            {
                encryptionService.EncryptValue(ref stringToEncrypt, context);

                member.SetValue(message, stringToEncrypt);
                return;
            }

            var legacyWireEncryptedString = valueToEncrypt as WireEncryptedString;
            if (legacyWireEncryptedString != null)
            {
                encryptionService.EncryptValue(legacyWireEncryptedString, context);
                return;
            }

            throw new Exception("Only string properties are supported for convention based encryption. Check the configured conventions.");
        }

        IEncryptionService encryptionService;
        EncryptionInspector messageInspector;

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration(EncryptionInspector inspector, IEncryptionService encryptionService)
                : base("MessagePropertyEncryption", typeof(EncryptBehavior), "Invokes the encryption logic", b => new EncryptBehavior(inspector, encryptionService))
            {
                InsertAfter("MutateOutgoingMessages");
            }
        }
    }
}