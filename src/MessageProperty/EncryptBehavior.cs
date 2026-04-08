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

            var propertiesToRestore = new List<Tuple<object, Tuple<object, MemberInfo>>>();
            var propertiesToEncrypt = messageInspector.ScanObject(currentMessageToSend);

            try
            {
                foreach (var item in propertiesToEncrypt)
                {
                    var oldValue = EncryptMember(item.Item1, item.Item2, context);

                    propertiesToRestore.Add(new Tuple<object, Tuple<object, MemberInfo>>(oldValue, item));
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
                        propertyToRestore.Item2.Item2.SetValue(propertyToRestore.Item2.Item1, propertyToRestore.Item1);
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
                var copy = new EncryptedString { Value = wireEncryptedString.Value };
                encryptionService.EncryptValue(wireEncryptedString, context);
                return copy;
            }

            if (valueToEncrypt is string stringToEncrypt)
            {
                var copy = stringToEncrypt;
                encryptionService.EncryptValue(ref stringToEncrypt, context);

                member.SetValue(message, stringToEncrypt);
                return copy;
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