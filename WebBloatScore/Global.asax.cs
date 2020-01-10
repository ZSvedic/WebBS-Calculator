using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WebBloatScore
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Removing WebForms view engine.
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine() { FileExtensions = new string[] { "cshtml" } });

            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            CleanerConfig.RegisterCleaner();
        }

        protected void Application_Error(object sender, EventArgs e) { Logger.Error(Server.GetLastError()); }
    }
}