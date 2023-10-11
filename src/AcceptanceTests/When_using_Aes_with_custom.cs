namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_Aes_with_custom : NServiceBusAcceptanceTest
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
                .Done(c => c.GetTheMessage)
                .Run();

            Assert.AreEqual("betcha can't guess my secret", context.Secret);
            Assert.AreEqual("My sub secret", context.SubPropertySecret);
            CollectionAssert.AreEquivalent(new List<string>
            {
                "312312312312312",
                "543645546546456"
            }, context.CreditCards);
        }

        public class Context : ScenarioContext
        {
            public bool GetTheMessage { get; set; }

            public string Secret { get; set; }

            public string SubPropertySecret { get; set; }

            public List<string> CreditCards { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var keys = new Dictionary<string, byte[]>
                {
                    {"1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
                };

                EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new AesEncryptionService("1st", keys)));
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

                    testContext.GetTheMessage = true;

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
}