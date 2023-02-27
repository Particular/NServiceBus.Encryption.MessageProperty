namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_sending_from_Aes_to_Rijndael : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_decrypted_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(session => session.Send(new MessageWithSecretData
                {
                    Secret = "betcha can't guess my secret"
                })))
                .WithEndpoint<Receiver>()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual("betcha can't guess my secret", context.Secret);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string Secret { get; set; }
        }

        static Dictionary<string, byte[]> Keys = new Dictionary<string, byte[]>
        {
            {"1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")}
        };

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.EnableMessagePropertyEncryption(new AesEncryptionService("1st", Keys));
                    builder.ConfigureRouting()
                        .RouteToEndpoint(typeof(MessageWithSecretData), Conventions.EndpointNamingConvention(typeof(Receiver)));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new RijndaelEncryptionService("1st", Keys)));
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
                    testContext.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageWithSecretData : IMessage
        {
            public EncryptedString Secret { get; set; }
        }
    }
}