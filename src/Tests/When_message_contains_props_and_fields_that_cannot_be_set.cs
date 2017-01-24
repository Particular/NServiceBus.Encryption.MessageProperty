﻿namespace NServiceBus.Core.Tests.Encryption
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_message_contains_props_and_fields_that_cannot_be_set : WireEncryptedStringContext
    {
        [Test]
        public void Should_ignore_those_properties_and_fields()
        {
            var message = new BogusEntityMessage{ Entity = new BogusEntity()};

            Assert.IsEmpty(inspector.ScanObject(message));
        }

        public class BogusEntityMessage : IMessage
        {
            public BogusEntity Entity { get; set; }
        }

        public class BogusEntity
        {
            //This field generates a stackoverflow

            public BogusEntity()
            {
                ExposesReadOnlyField = "Foo";
            }

            public string ExposesReadOnlyField { get; private set; }

            //This property generates a stackoverflow
            public List<BogusEntity> ExposesGetOnlyProperty => new List<BogusEntity> { new BogusEntity() };
        }
    }
}