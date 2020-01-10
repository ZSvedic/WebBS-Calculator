using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using WebBloatScore.Models;

namespace WebBloatScore.Controllers
{
    public class HomeController : Controller
    {
        private static readonly int RateLimit = Utilities.RateLimit;

        /* Average slimer execution time in ms:
         * (slimerjs.waitResource + slimerjs.maxWaitResource) / 2 =
         * (1000 + 30000) / 2 = 15500
         * 
         * Avarage pngquant execution time in ms:
         * (0 + Utilities.PngQuantWaitForExit) / 2 =
         * (0 + 20000) / 2 = 10000
         * 
         * Average execution count per min:
         * 60000 / (15500 + 10000) = 2.35 */
        private const double ExecutionCount = 2.35;

        // Take simultaneous executions into account (RateLimit)
        // and round a number up to nearest multiplier of 5.
        private static readonly int ScreenshotsPerMinute = ((int)Math.Ceiling(RateLimit * ExecutionCount / 5) * 5);

        private static int RateCounter = 0;

        [HttpGet, OutputCache(Duration = int.MaxValue)]
        public ActionResult Index(Uri url)
        {
            this.ViewBag.ScreenshotsPerMinute = ScreenshotsPerMinute;
            if (url == null || !IsValidUrl(url))
            {
                this.ViewBag.OgShare = false;
                this.ViewBag.OgUrl = "http://www.webbloatscore.com/";
                this.ViewBag.OgDescription = "Use WebBloatScore.com to calculate the Bloat Score of any website.";
            }
            else
            {
                this.ViewBag.OgShare = true;
                this.ViewBag.OgUrl = "http://www.webbloatscore.com?url=" + Uri.EscapeUriString(url.ToString());
                this.ViewBag.OgDescription = "Use WebBloatScore.com to calculate the Bloat Score of: " + Uri.EscapeUriString(url.ToString());
            }
            return this.View();
        }

        [HttpGet]
        public ActionResult Details(string id)
        {
            Logger.Info("Details Start => ID:" + id);
            
            var details = new DetailsResultCollection(id.ToString());
            if (details.Count == 0)
            {
                Logger.Warning("Details Empty => ID:" + id);
                throw new HttpException(404, string.Empty);
            }

            return this.View(details);
        }

        [HttpPost]
        public ActionResult Capture(Uri url, int timeout = 60)
        {
            // timeout should be between 20 and 600 seconds
            timeout = Math.Min(600, Math.Max(20, timeout)) * 1000;
            Logger.Info("Capture Start => URL:" + url + ", timeout: " + timeout);

            if (!IsValidUrl(url))
                return this.Fail(HttpStatusCode.BadRequest, "Capture Invalid => URL:" + url);
            if (IsRateLimitReached())
                return this.Fail((HttpStatusCode)429, "Capture Busy => URL:" + url);

            return this.GetCaptureResult(url, timeout);
        }

        private ActionResult GetCaptureResult(Uri url, int timeout)
        {
            if (IsCached(url))
            {
                Thread.Sleep(5000); // Don't return the result immediately so that it is not suspicious
                return GetFromCache(url);
            }

            CaptureResult result;

            int slimmerWaitForExit = IsCachableUrl(url) ? Utilities.SlimerWaitForExitWhenCaching : timeout / 4 * 3;
            int pngQuantWaitForExit = IsCachableUrl(url) ? Utilities.PngQuantWaitForExitWhenCaching : timeout / 4;

            Interlocked.Increment(ref RateCounter);
            try
            {
                SlimerExecutor executor = new SlimerExecutor(slimmerWaitForExit, pngQuantWaitForExit);
                result = executor.CalculateResult(url.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref RateCounter);
            }

            switch (result.Exit)
            {
                case ExitCode.Success:
                    if (IsCachableUrl(url))
                        CacheResult(url, result);
                    return this.Success(result, "Capture Success => URL:" + url);
                case ExitCode.SuccessTimeout:
                    return this.Success(result, "Capture Success Timeout => URL:" + url);
                case ExitCode.SuccessNotOptimized:
                    return this.Success(result, "Capture Success Not Optimized => URL:" + url);
                case ExitCode.FailTimeout:
                    return this.Fail(HttpStatusCode.GatewayTimeout, "Capture Fail Timeout => URL:" + url);
                default:
                    return this.Fail(HttpStatusCode.InternalServerError, "Capture Fail => URL:" + url);
            }
        }

        private ActionResult Success(CaptureResult result, string message)
        {
            Logger.Info(message);
            return this.Json(result);
        }

        private ActionResult Fail(HttpStatusCode code, string message)
        {
            Logger.Warning(message);
            this.Response.StatusCode = (int)code;
            return this.Content(string.Empty);
        }

        private static bool IsValidUrl(Uri url)
        {
            return url.IsAbsoluteUri && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
        }

        private static bool IsRateLimitReached()
        {
            return RateCounter >= RateLimit;
        }

        private bool IsCachableUrl(Uri url)
        {
            return Utilities.CachedUris.Contains(url.ToString());
        }

        private bool IsCached(Uri url)
        {
            return IsCachableUrl(url) && this.HttpContext.Cache[url.ToString()] != null;
        }

        private ActionResult GetFromCache(Uri url)
        {
            return this.Success((CaptureResult)this.HttpContext.Cache[url.ToString()], "Capture Success => URL:" + url);
        }

        private void CacheResult(Uri url, CaptureResult result)
        {
            // rename files so that they are not removed by cleaning
            string oldImagePath = Path.Combine(Utilities.ScreenshotsPath, result.ImagePath);
            string oldDetalisPath = Path.Combine(Utilities.ScreenshotsPath, result.DetailsPath);
            string newImagePath = oldImagePath.Insert(oldImagePath.LastIndexOf('\\') + 1, Utilities.CachedItemNameStart); // change ...\Screenshots\guid to ...\Screenshots\cached_guid
            string newDetailsPath = oldDetalisPath.Insert(oldDetalisPath.LastIndexOf('\\') + 1, Utilities.CachedItemNameStart);

            System.IO.File.Move(oldImagePath, newImagePath);
            System.IO.File.Move(oldDetalisPath, newDetailsPath);

            result.Image = result.Image.Insert(result.Image.LastIndexOf('/') + 1, Utilities.CachedItemNameStart);
            result.DetailsPath = Utilities.CachedItemNameStart + result.DetailsPath;

            this.HttpContext.Cache.Insert(url.ToString(), result, null, DateTime.Now.AddDays(Utilities.CachedScreenshotsLifetime), Cache.NoSlidingExpiration);
        }
    }
}
