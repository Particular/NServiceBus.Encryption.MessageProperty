namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Message convention definitions.
    /// </summary>
    public class Conventions
    {
        /// <summary>
        /// Returns true if the given property should be encrypted.
        /// </summary>
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

        /// <summary>
        /// The convention.
        /// </summary>
        public Func<PropertyInfo, bool> IsEncryptedPropertyAction {get; set;} = p => typeof(WireEncryptedString).IsAssignableFrom(p.PropertyType);
    }
}