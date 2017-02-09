namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_legacy_type : NServiceBusAcceptanceTest
    {
        const string keyIdentifier = "key";
        static readonly byte[] encryptionKey = Encoding.UTF8.GetBytes("1234567890abcdefghijKLMN");

        [Test]
        public async Task Should_encrypt_legacy_type()
        {
            const string secretValue = "the cake is a lie";
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(e => e
                    .When(s => s.Send(new MessageWithLegacyEncryptedPropertyType { Value = secretValue })))
                .WithEndpoint<ReceivingEndpoint>()
                .Done(c => c.ReceivedValue != null)
                .Run();

            Assert.AreEqual(secretValue, context.ReceivedValue);
        }

        class Context : ScenarioContext
        {
            public string ReceivedValue { get; set; }
        }

        class SendingEndpoint : EndpointConfigurationBuilder
        {
            public SendingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        // NServiceBus.Encryption.MessageProperty encryption feature
                        c.EnableMessagePropertyEncryption(new RijndaelEncryptionService(keyIdentifier, encryptionKey));
                    })
                    .AddMapping<MessageWithLegacyEncryptedPropertyType>(typeof(ReceivingEndpoint));
            }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    // NServiceBus.Core encryption feature
                    c.RijndaelEncryptionService(keyIdentifier, encryptionKey);
                });
            }

            public class MessageHandler : IHandleMessages<MessageWithLegacyEncryptedPropertyType>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithLegacyEncryptedPropertyType message, IMessageHandlerContext context)
                {
                    testContext.ReceivedValue = message.Value;
                    return Task.FromResult(0);
                }
            }
        }

        class MessageWithLegacyEncryptedPropertyType : ICommand
        {
            public WireEncryptedString Value { get; set; }
        }
    }
}