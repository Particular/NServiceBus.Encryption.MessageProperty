namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class WireEncryptedStringTests
    {
        [Test]
        public void Should_throw_an_exception()
        {
            var service = new FakeEncryptionService(new EncryptedValue
            {
                EncryptedBase64Value = "EncryptedBase64Value",
                Base64Iv = "Base64Iv"
            });

            var value = new EncryptedString
            {
                Value = "The real value"
            };

            // ReSharper disable once InvokeAsExtensionMethod
            var exception = Assert.Throws<Exception>(() => EncryptedStringConversions.DecryptValue(service, value, null));
            Assert.AreEqual("Encrypted property is missing encryption data", exception.Message);
        }
    }
}