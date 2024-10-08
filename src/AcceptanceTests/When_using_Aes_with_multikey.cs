﻿namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_using_Aes_with_multikey : NServiceBusAcceptanceTest
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

            Assert.That(context.Secret, Is.EqualTo("betcha can't guess my secret"));
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
                    builder.EnableMessagePropertyEncryption(new AesEncryptionService("1st", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")));
                    builder.ConfigureRouting()
                        .RouteToEndpoint(typeof(MessageWithSecretData), Conventions.EndpointNamingConvention(typeof(Receiver)));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                var key = Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
                var keys = new Dictionary<string, byte[]>
                {
                    {"2nd", Encoding.ASCII.GetBytes("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6")},
                    {"1st", key}
                };

                var expiredKeys = new[]
                {
                    key
                };
                EndpointSetup<DefaultServer>(builder => builder.EnableMessagePropertyEncryption(new AesEncryptionService("2nd", keys, expiredKeys)));
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