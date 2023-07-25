using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace CrossWord.Scraper
{
    public static class ChromeDriverUtils
    {
        public static IWebDriver GetChromeDriver(bool isHeadlessOnWindows = false)
        {
            var outPutDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var chromeDriverPath = outPutDirectory;
            string driverExecutableFileName = null;

            ChromeOptions options = new ChromeOptions();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                chromeDriverPath = "/usr/local/bin/";
                driverExecutableFileName = "chromedriver";

                options.AddArguments("--headless");
                // options.AddArguments("--disable-gpu"); // used to be required for headless on Windows but not anylonger, see crbug.com/737678.
                options.AddArguments("--no-sandbox"); // no-sandbox is not needed if you properly setup a user in the Linux container. See https://github.com/ebidel/lighthouse-ci/blob/master/builder/Dockerfile#L35-L40
							// however on CentOS it is for some reason needed anyway
                options.AddArguments("--whitelisted-ips='127.0.0.1'"); // to remove error messages "[SEVERE]: bind() returned an error, errno=99: Cannot assign requested address (99)"
                options.AddArguments("--disable-extensions");
                options.AddArguments("--window-size=1920,1080");
                options.AddArguments("--blink-settings=imagesEnabled=false"); // disable images
                options.AddArguments("--disable-dev-shm-usage"); // DevToolsActivePort file doesn't exist on CentOS
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // the nuget package Selenium.Chrome.WebDriver does not 
                // support the latest chrome versions - so use manual download instead:
                // scoop install chromedriver
                // ^ this installs C:\Users\<username>\scoop\shims\chromedriver.exe
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                chromeDriverPath = userPath + "/scoop/shims/";

                driverExecutableFileName = "chromedriver.exe";
                if (isHeadlessOnWindows) options.AddArguments("--headless");
                options.AddArguments("--window-size=1920,1080");
                options.AddArguments("--blink-settings=imagesEnabled=false"); // disable images
            }

            ChromeDriverService service = ChromeDriverService.CreateDefaultService(chromeDriverPath, driverExecutableFileName);
            // service.Port = 9515;
            service.WhitelistedIPAddresses = "127.0.0.1"; // to remove error messages "[SEVERE]: bind() -- see above
                                                          // service.EnableVerboseLogging = true;

            IWebDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(60));
            // driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30); // this make findelement throw a timeout error if it doesn't exist 
            // driver.Manage().Window.Maximize();

            // IWebDriver driver = new ChromeDriver(chromeDriverPath, options);

            Log.Information("Using chromedriver path: '{0}', options: {1}", chromeDriverPath, options);

            return driver;
        }

        public static void KillAllChromeDriverInstances(bool doKillChromeOnWindows = false)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var cmd = "taskkill /f /im chromedriver.exe";
                var escapedArgs = cmd.Replace("\"", "\\\"");

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C \"{escapedArgs}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                Log.Information("Killing Chromedriver on Windows: '{0} {1}'", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Log.Debug("Killing Chromedriver on Windows: '{0}'", result);

                process.Close();


                // also kill Chrome
                if (doKillChromeOnWindows)
                {
                    cmd = "taskkill /f /im chrome.exe";
                    escapedArgs = cmd.Replace("\"", "\\\"");

                    process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C \"{escapedArgs}\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };

                    Log.Information("Killing Chrome.exe on Windows: '{0} {1}'", process.StartInfo.FileName, process.StartInfo.Arguments);

                    process.Start();
                    result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    Log.Debug("Killing Chrome.exe on Windows: '{0}'", result);

                    process.Close();
                }

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var cmd = "pkill chrome";

                var escapedArgs = cmd.Replace("\"", "\\\"");

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{escapedArgs}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                Log.Information("Killing Chromedriver on Linux: '{0} {1}'", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Log.Debug("Killing Chromedriver on Linux: '{0}'", result);

                process.Close();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

            }
        }
    }
}
