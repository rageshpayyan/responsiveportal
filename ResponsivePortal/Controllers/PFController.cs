using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ResponsivePortal.Controllers
{
    public class PFController : Controller
    {
        [OutputCache(Location = System.Web.UI.OutputCacheLocation.Client, Duration = 86400, VaryByParam = "rand")]
        public ActionResult Index(int clientID, int portalID, string PathEnd)
        {
            string path = HttpContext.Application["KBDataPath"] + "knowledgebase\\publicfiles\\" + clientID + "\\" + PathEnd.Replace("/", "\\");
            return new ResponsivePortal.Models.FileResult(path);
        }
	}
}