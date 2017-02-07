namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Message convention definitions.
    /// </summary>
    class IsEncryptedPropertyConvention
    {
        public IsEncryptedPropertyConvention(Func<PropertyInfo, bool> isEncryptedPropertyAction)
        {
            IsEncryptedPropertyAction = isEncryptedPropertyAction;
        }

        public bool IsEncryptedProperty(PropertyInfo property)
        {
            Guard.AgainstNull(nameof(property), property);
            try
            {
                //the message mutator will cache the whole message so we don't need to cache here
                return IsEncryptedPropertyAction(property);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Encrypted Property convention. See inner exception for details.", ex);
            }
        }

        Func<PropertyInfo, bool> IsEncryptedPropertyAction;
    }
}