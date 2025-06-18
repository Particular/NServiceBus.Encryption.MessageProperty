namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Pipeline;

    public class When_using_encryption_with_custom_service : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_decrypted_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new MessageWithSecretData
                {
                    Secret = "betcha can't guess my secret",
                    SubProperty = new MySecretSubProperty
                    {
                        Secret = "My sub secret"
                    },
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
                })))
                .Done(c => c.GotTheMessage)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.Secret, Is.EqualTo("betcha can't guess my secret"));
                Assert.That(context.SubPropertySecret, Is.EqualTo("My sub secret"));
            });
            Assert.That(context.CreditCards, Is.EquivalentTo(
            [
                "312312312312312",
                "543645546546456"
            ]));
        }

        public class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }

            public string Secret { get; set; }

            public string SubPropertySecret { get; set; }

            public List<string> CreditCards { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new MyEncryptionService()));
            }

            public class Handler : IHandleMessages<MessageWithSecretData>
            {
                Context testContext;

                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithSecretData message, IMessageHandlerContext context)
                {
                    testContext.Secret = message.Secret.Value;

                    testContext.SubPropertySecret = message.SubProperty.Secret.Value;

                    testContext.CreditCards =
                    [
                        message.CreditCards[0].Number.Value,
                        message.CreditCards[1].Number.Value
                    ];

                    testContext.GotTheMessage = true;

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

        public class MyEncryptionService : IEncryptionService
        {
            public EncryptedValue Encrypt(string value, IOutgoingLogicalMessageContext context)
            {
                return new EncryptedValue
                {
                    EncryptedBase64Value = new string(value.Reverse().ToArray())
                };
            }

            public string Decrypt(EncryptedValue encryptedValue, IIncomingLogicalMessageContext context)
            {
                return new string(encryptedValue.EncryptedBase64Value.Reverse().ToArray());
            }
        }
    }
}