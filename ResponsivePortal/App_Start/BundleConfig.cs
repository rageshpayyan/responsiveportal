using System.Web;
using System.Web.Optimization;
using System.Text;

namespace ResponsivePortal
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
            //            "~/Scripts/jquery-{version}.js",
            //            "~/Scripts/jquery-ui-{version}.js",
            //            "~/Scripts/jquery.*",
            //            "~/Scripts/jquery.js",
            //            "~/Scripts/placeholder.js",
            //            "~/Scripts/responde.min.js"
            //           ));

           

            //// Use the development version of Modernizr to develop with and learn from. Then, when you're
            //// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            //bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
            //            "~/Scripts/modernizr-*"));

            //#region Foundation Bundles              
            ////bundles.Add(new StyleBundle("~/Content/foundation/css").Include(
            ////          "~/Content/foundation/foundation.css",
            ////          "~/Content/foundation/foundation.mvc.css"                    
            ////          ));
            //bundles.Add(new ScriptBundle("~/bundles/foundation").Include(
            //         "~/Scripts/foundation/foundation.js",
            //         "~/Scripts/foundation/foundation.*"
            //         ));
            //#endregion

            //#region KB configuration Bundles     
            //StringBuilder sb = new StringBuilder();
            //sb.Append("~/Content/");
            //sb.Append(Settings.CSS_CONFIG_FOLDER);
            //sb.Append("/");
            //sb.Append(Settings.DEFAULT_CLIENTID);
            //sb.Append("/");
            //sb.Append(Settings.DEFAULT_PORTALID);
            //sb.Append("/");
            //sb.Append(Settings.CONFIG_CUST_STULES);
            //bundles.Add(new StyleBundle("~/Content/css").Include(                     
            //          sb.ToString()));

            //sb = new StringBuilder();
            //sb.Append("~/");
            //sb.Append(Settings.JS_CONFIG_FOLDER);
            //sb.Append("/");
            //sb.Append(Settings.DEFAULT_CLIENTID);
            //sb.Append("/");
            //sb.Append(Settings.DEFAULT_PORTALID);
            //sb.Append("/");
            //sb.Append(Settings.CONFIG_CUST_RESIZESCRIPT);
            //bundles.Add(new StyleBundle("~/Content/js").Include(
            //          sb.ToString()));
            //#endregion
        }
    }
}
