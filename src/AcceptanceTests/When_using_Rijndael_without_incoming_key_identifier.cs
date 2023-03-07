namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using MessageMutator;
    using NUnit.Framework;

    public class When_using_Rijndael_without_incoming_key_identifier : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_process_decrypted_message_without_key_identifier()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(bus => bus.Send(new MessageWithSecretData
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

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    builder.EnableMessagePropertyEncryption(new RijndaelEncryptionService("will-be-removed-by-transport-mutator", Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
#pragma warning restore CS0618 // Type or member is obsolete
                    builder.ConfigureRouting()
                        .RouteToEndpoint(typeof(MessageWithSecretData), Conventions.EndpointNamingConvention(typeof(Receiver)));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var keys = new Dictionary<string, byte[]>
                {
                    {"new", Encoding.ASCII.GetBytes("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")}
                };

                var expiredKeys = new[]
                {
                    Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                };
                EndpointSetup<DefaultServer>(builder =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    builder.EnableMessagePropertyEncryption(new RijndaelEncryptionService("new", keys, expiredKeys));
#pragma warning restore CS0618 // Type or member is obsolete
                    builder.RegisterMessageMutator(new RemoveKeyIdentifierHeaderMutator());
                });
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

        class RemoveKeyIdentifierHeaderMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                context.Headers.Remove(EncryptionHeaders.RijndaelKeyIdentifier);
                return Task.FromResult(0);
            }
        }
    }
}