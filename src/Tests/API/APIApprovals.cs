namespace NServiceBus.Encryption.MessageProperty.Tests
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(RijndaelEncryptionService).Assembly);
            Approver.Verify(publicApi);
        }
    }
}