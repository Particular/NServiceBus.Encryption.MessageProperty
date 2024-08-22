namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class Issue_949 : WireEncryptedStringContext
    {
        [Test]
        public void Null_element_in_primitive_array()
        {
            var message = new TestMessageWithPrimitives
            {
                Data = new int?[] { null, 1 }
            };

            inspector.ScanObject(message);

            Assert.That(message.Data, Is.EqualTo(new int?[] { null, 1 }));
        }

        [Test]
        public void Null_element_in_object_array()
        {
            var message = new TestMessageWithObjects
            {
                Data = new object[] { null, this, null }
            };

            inspector.ScanObject(message);

            Assert.That(message.Data, Is.EqualTo(new object[] { null, this, null }));

        }

        class TestMessageWithPrimitives
        {
            public int?[] Data;
        }

        class TestMessageWithObjects
        {
            public object[] Data;
        }
    }
}
