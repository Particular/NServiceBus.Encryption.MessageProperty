namespace NServiceBus.Encryption.MessageProperty.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using AcceptanceTesting;
    using NUnit.Framework;


    [TestFixture]
    public class When_enabling_core_encryption_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_at_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(() =>
                Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EncryptionEndpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run());

            StringAssert.Contains("The message property encryption extension as well as NServiceBus.Core's encryption feature are enabled. Disable one of the encryption features to avoid message payload corruption.", exception.Message);
        }

        class EncryptionEndpoint : EndpointConfigurationBuilder
        {
            public EncryptionEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    // ReSharper disable once InvokeAsExtensionMethod
                    c.EnableMessagePropertyEncryption(new RijndaelEncryptionService("keyIdentifier1", new Dictionary<string, byte[]>
                    {
                        {"keyIdentifier1", Encoding.ASCII.GetBytes("aaaaaaaaaabbbbbbbbbbcccc")}
                    }));

                    c.RijndaelEncryptionService("keyIdentifier2", new Dictionary<string, byte[]>
                    {
                        {"keyIdentifier2", Encoding.ASCII.GetBytes("ddddddddddeeeeeeeeeeffff")}
                    });
                });
            }
        }
    }
}