namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;

public class When_message_or_sub_properties_are_reused_encrypted_string : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_double_encrypt_properties()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(async session =>
            {
                var messageToReuse = new MessageWithSecretData
                {
                    Secret = "betcha can't guess my secret",
                    SubProperty = new MySecretSubProperty { Secret = "My sub secret" }
                };
                await session.SendLocal(messageToReuse);
                await session.SendLocal(messageToReuse);
            }))
            .Run();

        Assert.Multiple(() =>
        {
            foreach (var messageToReuse in context.MessagesReceived)
            {
                Assert.That(messageToReuse.Secret.Value, Is.EqualTo("betcha can't guess my secret"));
                Assert.That(messageToReuse.SubProperty.Secret.Value, Is.EqualTo("My sub secret"));
            }
        });
    }

    public class Context : ScenarioContext
    {
        public List<MessageWithSecretData> MessagesReceived { get; } = [];
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            var keys = new Dictionary<string, byte[]> { { "1st", "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"u8.ToArray() } };

            EndpointSetup<DefaultServer>(config => config.EnableMessagePropertyEncryption(new AesEncryptionService("1st", keys)));
        }

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