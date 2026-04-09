namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;

public class When_sending_the_same_message_twice : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_corrupt_encrypted_properties()
    {
        var messageToReuse = new MessageWithSecretData
        {
            Secret = "betcha can't guess my secret",
            SubProperty = new MySecretSubProperty { Secret = "My sub secret" }
        };

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(async session =>
            {
                await session.SendLocal(messageToReuse);
                await session.SendLocal(messageToReuse);
            }))
            .Run();

        Assert.Multiple(() =>
        {
            foreach (var message in context.MessagesReceived)
            {
                Assert.That(message.Secret.Value, Is.EqualTo(messageToReuse.Secret.Value));
                Assert.That(message.SubProperty.Secret.Value, Is.EqualTo(messageToReuse.SubProperty.Secret.Value));
            }
        });
    }

    public class Context : ScenarioContext
    {
        public List<MessageWithSecretData> MessagesReceived { get; } = [];
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new AesEncryptionService("1st", new Dictionary<string, byte[]> { { "1st", "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"u8.ToArray() } })));

        public class Handler(Context testContext) : IHandleMessages<MessageWithSecretData>
        {
            public Task Handle(MessageWithSecretData message, IMessageHandlerContext context)
            {
                testContext.MessagesReceived.Add(message);
                testContext.MarkAsCompleted(testContext.MessagesReceived.Count == 2);

                return Task.FromResult(0);
            }
        }
    }

    public class MessageWithSecretData : IMessage
    {
        public EncryptedString Secret { get; set; }
        public MySecretSubProperty SubProperty { get; set; }
    }

    public class MySecretSubProperty
    {
        public EncryptedString Secret { get; set; }
    }
}