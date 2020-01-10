using System.Web.Optimization;

namespace WebBloatScore
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(
                new StyleBundle("~/Content/Styles/css").Include(
                    "~/Content/Styles/font.css",
                    "~/Content/Styles/site.css"));

            bundles.Add(
                new ScriptBundle("~/Content/Scripts/js").Include(
                    "~/Content/Scripts/site.js"));
        }
    }
}