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

            if (valueToEncrypt is EncryptedString wireEncryptedString)
            {
                encryptionService.EncryptValue(wireEncryptedString, context);
                return;
            }

            if (valueToEncrypt is string stringToEncrypt)
            {
                encryptionService.EncryptValue(ref stringToEncrypt, context);

                member.SetValue(message, stringToEncrypt);
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