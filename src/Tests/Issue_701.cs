﻿namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class Issue_701 : WireEncryptedStringContext
    {
        [Test]
        public void No_get_on_property()
        {
            var message = new TestMessageWithSets
            {
                Name = "John"
            };

            var result = inspector.ScanObject(message).ToList();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void No_set_on_property()
        {
            var message = new TestMessageWithGets
            {
                Name = "John"
            };

            var result = inspector.ScanObject(message).ToList();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        class TestMessageWithSets
        {
            public string Name { get; set; }

            public string Options1
            {
                set
                {
                    //do nothing
                }
            }

            public int Options2
            {
                set
                {
                    //do nothing
                }
            }
        }

        class TestMessageWithGets
        {
            public string Name { get; set; }

            public string Options1 => "Testing";

            public int Options2 => 5;
        }
    }
}