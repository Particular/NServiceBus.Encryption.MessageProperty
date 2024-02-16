namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Message convention definitions.
    /// </summary>
    class IsEncryptedPropertyConvention(Func<PropertyInfo, bool> isEncryptedPropertyAction)
    {
        public bool IsEncryptedProperty(PropertyInfo property)
        {
            ArgumentNullException.ThrowIfNull(property, nameof(property));

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

        readonly Func<PropertyInfo, bool> IsEncryptedPropertyAction = isEncryptedPropertyAction;
    }
}