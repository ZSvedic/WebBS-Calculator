using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web;

namespace WebBloatScore
{
    public static class Utilities
    {
        public const int CachedScreenshotsLifetime = 30; // 30 days
        public const int SlimerWaitForExitWhenCaching = 90000;
        public const int PngQuantWaitForExitWhenCaching = 40000;
        public const string CachedItemNameStart = "cache_"; // start name of the cached screenshots

        public static readonly IList<string> CachedUris = new List<string>
        {
            "https://www.w3.org/People/Berners-Lee/",
            "https://www.google.com/ncr#q=web+bloat",
            "https://www.amazon.com/",
            "https://www.webbloatscore.com/",
            "https://edition.cnn.com/"
        };

        private static readonly string ScreenshotsDir = ConfigurationManager.AppSettings["ScreenshotsDir"];

        public static readonly string ScreenshotsPath = GetApsolutePath(ScreenshotsDir);
        public static readonly string ScreenshotsRelativePath = VirtualPathUtility.ToAbsolute(ScreenshotsDir);
        public static readonly double ScreenshotsLifetime = double.Parse(ConfigurationManager.AppSettings["ScreenshotsLifetime"], CultureInfo.InvariantCulture);

        public static readonly string SlimerExecutable = GetApsolutePath(ConfigurationManager.AppSettings["SlimerExe"]);
        public static readonly string SlimerSource = GetApsolutePath(ConfigurationManager.AppSettings["SlimerJs"]);
        public static readonly string SlimerUserAgent = ConfigurationManager.AppSettings["SlimerUserAgent"];

        public static readonly string PngQuantExecutable = GetApsolutePath(ConfigurationManager.AppSettings["PngQuantExe"]);
        public static readonly string PngQuantArguments = GetApsolutePath(ConfigurationManager.AppSettings["PngQuantArgs"]);

        public static string LoggerFile { get { return GetApsolutePath(ConfigurationManager.AppSettings["LoggerFile"]); } }
        public static string LoggerLevel { get { return ConfigurationManager.AppSettings["LoggerLevel"]; } }

        public static int RateLimit
        {
            get
            {
                string virtualMachine;
                switch (Environment.ProcessorCount)
                {
                    case 2:
                        virtualMachine = "A2";
                        break;
                    case 4:
                        virtualMachine = "A3";
                        break;
                    case 8:
                        virtualMachine = "A4";
                        break;
                    default:
                        virtualMachine = "A1";
                        break;
                }
                return int.Parse(ConfigurationManager.AppSettings[virtualMachine + "_RateLimit"], CultureInfo.InvariantCulture);
            }
        }

        private static string GetApsolutePath(string relativePath) { return HttpContext.Current.Server.MapPath(relativePath); }
    }
}