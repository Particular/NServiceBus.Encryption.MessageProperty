﻿namespace NServiceBus.Encryption.Tests
{
    using Core.Tests.Encryption;
    using NUnit.Framework;

    [TestFixture]
    public class Issue_949 : WireEncryptedStringContext
    {
        [Test]
        public void null_element_in_primitive_array()
        {
            var message = new TestMessageWithPrimitives
            {
                Data = new int?[] { null, 1 }
            };

            inspector.ScanObject(message);

            Assert.AreEqual(new int?[] { null, 1 }, message.Data);
        }

        [Test]
        public void null_element_in_object_array()
        {
            var message = new TestMessageWithObjects
            {
                Data = new object[] { null, this, null }
            };

            inspector.ScanObject(message);

            Assert.AreEqual(new object[] { null, this, null }, message.Data);

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
