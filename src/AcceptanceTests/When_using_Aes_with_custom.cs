namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using NUnit.Framework;

public class When_using_Aes_with_custom : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_receive_decrypted_message()
    {
        var messageToSend = new MessageWithSecretData
        {
            Secret = "betcha can't guess my secret",
            SubProperty = new MySecretSubProperty { Secret = "My sub secret" },
            CreditCards =
            [
                new CreditCardDetails
                {
                    ValidTo = DateTime.UtcNow.AddYears(1),
                    Number = "312312312312312"
                },
                new CreditCardDetails
                {
                    ValidTo = DateTime.UtcNow.AddYears(2),
                    Number = "543645546546456"
                }
            ]
        };

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(messageToSend)))
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.Secret, Is.EqualTo(messageToSend.Secret.Value));
            Assert.That(context.SubPropertySecret, Is.EqualTo(messageToSend.SubProperty.Secret.Value));
            Assert.That(context.CreditCards, Is.EquivalentTo(
            [
                "312312312312312",
                "543645546546456"
            ]));
        });
    }

    public class Context : ScenarioContext
    {
        public string Secret { get; set; }

        public string SubPropertySecret { get; set; }

        public List<string> CreditCards { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            var keys = new Dictionary<string, byte[]> { { "1st", "gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6"u8.ToArray() } };

            EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new AesEncryptionService("1st", keys)));
        }

        public class Handler(Context testContext) : IHandleMessages<MessageWithSecretData>
        {
            public Task Handle(MessageWithSecretData message, IMessageHandlerContext context)
            {
                testContext.Secret = message.Secret.Value;

                testContext.SubPropertySecret = message.SubProperty.Secret.Value;

                testContext.CreditCards =
                [
                    message.CreditCards[0].Number.Value,
                    message.CreditCards[1].Number.Value
                ];

                testContext.MarkAsCompleted();

                return Task.FromResult(0);
            }
        }
    }

    public class MessageWithSecretData : IMessage
    {
        public EncryptedString Secret { get; set; }
        public MySecretSubProperty SubProperty { get; set; }
        public List<CreditCardDetails> CreditCards { get; set; }
    }

    public class CreditCardDetails
    {
        public DateTime ValidTo { get; set; }
        public EncryptedString Number { get; set; }
    }

    public class MySecretSubProperty
    {
        public EncryptedString Secret { get; set; }
    }
}