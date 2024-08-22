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

            var exception = Assert.Throws<Exception>(() => EncryptedStringConversions.DecryptValue(service, value, null));
            Assert.That(exception.Message, Is.EqualTo("Encrypted property is missing encryption data"));
        }
    }
}