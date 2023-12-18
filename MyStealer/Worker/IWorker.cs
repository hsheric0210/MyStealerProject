using System.Collections.Generic;

namespace MyStealer.Worker
{
    internal interface IWorker
    {
        string Name { get; }
        bool Check();
        ISet<Credential> GetCredentials();

    }
}
