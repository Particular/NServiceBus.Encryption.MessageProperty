namespace NServiceBus.Encryption.MessageProperty
{
    /// <summary>
    /// An <see cref="IEncryptionService"/> implementation usable for message property encryption using the RijndaelManaged algorithm.
    /// </summary>
    [ObsoleteEx(ReplacementTypeOrMember = "AesEncryptionService",
        TreatAsErrorFromVersion = "4",
        RemoveInVersion = "5")]
    public class RijndaelEncryptionService
    {
    }
}