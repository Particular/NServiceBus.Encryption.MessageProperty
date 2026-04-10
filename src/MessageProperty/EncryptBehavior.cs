namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    class EncryptBehavior(EncryptionInspector messageInspector, IEncryptionService encryptionService) : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public async Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var currentMessageToSend = context.Message.Instance;

            var propertiesToRestore = new List<(object unencryptedValue, object target, MemberInfo member)>();
            var propertiesToEncrypt = messageInspector.ScanObject(currentMessageToSend);

            try
            {
                foreach (var (target, member) in propertiesToEncrypt)
                {
                    var oldValue = EncryptMember(target, member, context);

                    propertiesToRestore.Add((oldValue, target, member));
                }

                context.UpdateMessage(currentMessageToSend);

                await next(context).ConfigureAwait(false);
            }
            finally
            {
                if (propertiesToEncrypt.Any())
                {
                    foreach (var propertyToRestore in propertiesToRestore)
                    {
                        propertyToRestore.member.SetValue(propertyToRestore.target, propertyToRestore.unencryptedValue);
                    }

                    context.UpdateMessage(currentMessageToSend);
                }
            }
        }

        object EncryptMember(object message, MemberInfo member, IOutgoingLogicalMessageContext context)
        {
            var valueToEncrypt = member.GetValue(message);

            if (valueToEncrypt is EncryptedString wireEncryptedString)
            {
                var unencryptedValue = new EncryptedString { Value = wireEncryptedString.Value };
                encryptionService.EncryptValue(wireEncryptedString, context);
                return unencryptedValue;
            }

            if (valueToEncrypt is string stringToEncrypt)
            {
                var unencryptedValue = stringToEncrypt;
                encryptionService.EncryptValue(ref stringToEncrypt, context);

                member.SetValue(message, stringToEncrypt);
                return unencryptedValue;
            }

            throw new Exception("Only string properties are supported for convention based encryption. Check the configured conventions.");
        }

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration(EncryptionInspector inspector, IEncryptionService encryptionService)
                : base("MessagePropertyEncryption", typeof(EncryptBehavior), "Invokes the encryption logic", b => new EncryptBehavior(inspector, encryptionService)) =>
                InsertAfter("MutateOutgoingMessages");
        }
    }
}