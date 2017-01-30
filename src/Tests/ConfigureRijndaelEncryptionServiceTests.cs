namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigureRijndaelEncryptionServiceTests
    {

        [Test]
        public void Should_not_throw_for_empty_keys()
        {
            ConfigureRijndaelEncryptionService.VerifyKeys(new List<string>());
        }

        [Test]
        public void Should_throw_for_overlapping_keys()
        {
            var keys = new List<string>
            {
                "key1",
                "key2",
                "key1"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            StringAssert.StartsWith("Overlapping keys defined. Ensure that no keys overlap.", exception.Message);
            Assert.AreEqual("expiredKeys", exception.ParamName);
        }

        [Test]
        public void Should_throw_for_whitespace_key()
        {
            var keys = new List<string>
            {
                "key1",
                "",
                "key2"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            StringAssert.StartsWith("Empty encryption key detected in position 1.", exception.Message);
            Assert.AreEqual("expiredKeys", exception.ParamName);
        }

        [Test]
        public void Should_throw_for_null_key()
        {
            var keys = new List<string>
            {
                "key1",
                null,
                "key2"
            };
            var exception = Assert.Throws<ArgumentException>(() => ConfigureRijndaelEncryptionService.VerifyKeys(keys));
            StringAssert.StartsWith("Empty encryption key detected in position 1.", exception.Message);
            Assert.AreEqual("expiredKeys", exception.ParamName);
        }
    }
}
