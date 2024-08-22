namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using NUnit.Framework;
    using Pipeline;

    [TestFixture]
    public class AesEncryptionServiceTest
    {
        [Test]
        public void Should_encrypt_and_decrypt()
        {
            var encryptionKey = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service = new TestableRijndaelEncryptionService("encryptionKey", encryptionKey, new[]
            {
                encryptionKey
            });
            var encryptedValue = service.Encrypt("string to encrypt", null);
            Assert.That(encryptedValue.EncryptedBase64Value, Is.Not.EqualTo("string to encrypt"));
            var decryptedValue = service.Decrypt(encryptedValue, null);
            Assert.That(decryptedValue, Is.EqualTo("string to encrypt"));
        }

        [Test]
        public void Should_encrypt_and_decrypt_for_expired_key()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey1, new[]
            {
                encryptionKey1
            })
            {
            };
            var encryptedValue = service1.Encrypt("string to encrypt", null);
            Assert.That(encryptedValue.EncryptedBase64Value, Is.Not.EqualTo("string to encrypt"));

            var encryptionKey2 = Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya");
            var service2 = new TestableRijndaelEncryptionService("encryptionKey2", encryptionKey2,
            [
                encryptionKey2,
                encryptionKey1
            ]);

            var decryptedValue = service2.Decrypt(encryptedValue, null);
            Assert.That(decryptedValue, Is.EqualTo("string to encrypt"));
        }

        [Ignore("flaky: https://github.com/Particular/NServiceBus/issues/4295")]
        [Test]
        public void Should_throw_when_decrypt_with_wrong_key()
        {
            var usedKey = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var service1 = new TestableRijndaelEncryptionService("should-be-ignored-in-next-arrange", usedKey, []);
            var encryptedValue = service1.Encrypt("string to encrypt", null);
            Assert.That(encryptedValue.EncryptedBase64Value, Is.Not.EqualTo("string to encrypt"));

            var unusedExpiredKeys = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                Encoding.ASCII.GetBytes("cccccccccccccccccccccccccccccccc")
            };

            var service2 = new TestableRijndaelEncryptionService("should-be-ignored", usedKey, unusedExpiredKeys);

            var exception = Assert.Throws<AggregateException>(() => service2.Decrypt(encryptedValue, null));
            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Is.EqualTo("Could not decrypt message. Tried 2 keys."));
                Assert.That(exception.InnerExceptions, Has.Count.EqualTo(2));
            });
            foreach (var inner in exception.InnerExceptions)
            {
                Assert.That(inner, Is.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Should_throw_for_invalid_key()
        {
            var invalidKey = Encoding.ASCII.GetBytes("invalidKey");
            var exception = Assert.Throws<Exception>(() => new TestableRijndaelEncryptionService("keyid", invalidKey, []));
            Assert.That(exception.Message, Is.EqualTo("The encryption key has an invalid length of 10 bytes."));
        }

        [Test]
        public void Should_throw_for_invalid_expired_key()
        {
            var validKey = Encoding.ASCII.GetBytes("adDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var expiredKeys = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("invalidKey")
            };
            var exception = Assert.Throws<Exception>(() => new TestableRijndaelEncryptionService("keyid", validKey, expiredKeys));
            Assert.That(exception.Message, Is.EqualTo("The expired key at index 0 has an invalid length of 10 bytes."));
        }

        [Test]
        public void Encrypt_must_set_header()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey1, []);

            Assert.That(service1.OutgoingKeyIdentifierSet, Is.EqualTo(false));
            service1.Encrypt("string to encrypt", null);
            Assert.That(service1.OutgoingKeyIdentifierSet, Is.EqualTo(true));
        }

        [Test]
        public void Decrypt_using_key_identifier()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey1, []);
            var encryptedValue = service1.Encrypt("string to encrypt", null);

            var expiredKeys = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")
            };
            var service2 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey1, expiredKeys)
            {
                IncomingKeyIdentifier = "encryptionKey1"
            };

            var decryptedValue = service2.Decrypt(encryptedValue, null);
            Assert.That(decryptedValue, Is.EqualTo("string to encrypt"));
        }

        [Test]
        public void Decrypt_using_missing_key_identifier_must_throw()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var service1 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey1, []);
            var encryptedValue = service1.Encrypt("string to encrypt", null);

            var encryptionKey2 = Encoding.ASCII.GetBytes("vznkynwuvateefgduvsqjsufqfrrfcya");
            var expiredKeys = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")
            };
            var service2 = new TestableRijndaelEncryptionService("encryptionKey1", encryptionKey2, expiredKeys)
            {
                IncomingKeyIdentifier = "missingKey"
            };

            Assert.Catch<InvalidOperationException>(() => { service2.Decrypt(encryptedValue, null); }, "Decryption key not available for key identifier 'missingKey'. Add this key to the rijndael encryption service configuration. Key identifiers are case sensitive.");
        }

        [Test]
        public void Encrypt_using_missing_key_identifier_must_throw()
        {
            var encryptionKey1 = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            Assert.Catch<ArgumentNullException>(() => new AesEncryptionService(null, new Dictionary<string, byte[]>
            {
                {"some-key", encryptionKey1}
            }, []));
        }

        [Test]
        public void Should_throw_when_passing_non_existing_key_identifier()
        {
            Assert.Catch<ArgumentException>(() => { new AesEncryptionService("not-in-keys", new Dictionary<string, byte[]>(), null); });
        }

        [Test]
        public void Should_throw_informative_exception_when_decryption_fails_with_key_identifier()
        {
            var keyIdentifier = "encryptionKey1";

            var key1 = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var service1 = new TestableRijndaelEncryptionService(keyIdentifier, key1, []);
            var encryptedValue = service1.Encrypt("string to encrypt", null);

            var key2 = Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            var service2 = new TestableRijndaelEncryptionService(keyIdentifier, key2, [])
            {
                IncomingKeyIdentifier = "encryptionKey1"
            };

            Assert.Catch<InvalidOperationException>(() => { service2.Decrypt(encryptedValue, null); }, "Unable to decrypt property using configured decryption key specified in key identifier header.");
        }

        class TestableRijndaelEncryptionService : AesEncryptionService
        {
            public TestableRijndaelEncryptionService(
                string encryptionKeyIdentifier,
                byte[] encryptionKey,
                IList<byte[]> decryptionKeys)
                : base(encryptionKeyIdentifier, new Dictionary<string, byte[]>
                {
                    {encryptionKeyIdentifier, encryptionKey}
                }, decryptionKeys)
            {
            }

            public bool OutgoingKeyIdentifierSet { get; private set; }
            public string IncomingKeyIdentifier { private get; set; }

            protected internal override void AddKeyIdentifierHeader(IOutgoingLogicalMessageContext context)
            {
                OutgoingKeyIdentifierSet = true;
            }

            protected internal override bool TryGetKeyIdentifierHeader(out string keyIdentifier, IIncomingLogicalMessageContext context)
            {
                keyIdentifier = IncomingKeyIdentifier;
                return IncomingKeyIdentifier != null;
            }
        }
    }
}