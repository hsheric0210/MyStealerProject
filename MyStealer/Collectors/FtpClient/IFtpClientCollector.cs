using System.Collections.Immutable;

namespace MyStealer.Collectors.FtpClient
{
    internal interface IMessengerCollector
    {
        IImmutableSet<CredentialEntry> GetCredentials();
    }
}
