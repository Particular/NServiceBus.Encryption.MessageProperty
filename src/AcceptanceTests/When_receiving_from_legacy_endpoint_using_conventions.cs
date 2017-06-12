namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_from_legacy_endpoint_using_conventions : NServiceBusAcceptanceTest
    {
        const string keyIdentifier = "key";
        static readonly byte[] encryptionKey = Encoding.UTF8.GetBytes("1234567890abcdefghijKLMN");

        [Test]
        public async Task Should_decrypt_received_message()
        {
            const string secretValue = "the cake is a lie";
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SendingEndpoint>(e => e
                    .When(s => s.Send(new MessageWithEncryptedProperty { Value = secretValue })))
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
                    // NServiceBus.Core encryption feature
                    c.RijndaelEncryptionService(keyIdentifier, encryptionKey);
                    c.Conventions().DefiningEncryptedPropertiesAs(p => p.Name == "Value");
                    c.ConfigureTransport()
                        .Routing()
                        .RouteToEndpoint(typeof(MessageWithEncryptedProperty), Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)));
                });
            }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    // NServiceBus.Encryption.MessageProperty encryption feature
                    c.EnableMessagePropertyEncryption(new RijndaelEncryptionService(keyIdentifier, encryptionKey), p => p.Name == "Value");
                });
            }

            public class MessageHandler : IHandleMessages<MessageWithEncryptedProperty>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithEncryptedProperty message, IMessageHandlerContext context)
                {
                    testContext.ReceivedValue = message.Value;
                    return Task.FromResult(0);
                }
            }
        }

        class MessageWithEncryptedProperty : ICommand
        {
            public string Value { get; set; }
        }
    }
}