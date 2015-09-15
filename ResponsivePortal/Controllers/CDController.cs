using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NLog;
using KBCommon.KBException;
using ResponsivePortal.Resources;
using ResponsivePortal.Filters.MVC;
namespace ResponsivePortal.Controllers
{
    [AuthorizeResourceAttribute]
    public class CDController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        [OutputCache(Location = System.Web.UI.OutputCacheLocation.Client, Duration = 86400, VaryByParam = "rand")]
        public ActionResult Index(int ClientID, int PortalID, string pathEnd)
        {


            string path = HttpContext.Application["KBDataPath"] + "knowledgebase\\customerData\\" + ClientID + "\\" + pathEnd.Replace("/", "\\");
            try
            {
                if (!pathEnd.ToLower().StartsWith("resources"))
                {
                    UnauthorizedAccessException unauthedAccessEx = new UnauthorizedAccessException(GeneralResources.ConvertCDPathError);
                    KBCustomException kbCustExp = KBCustomException.ProcessException(unauthedAccessEx, KBOp.ConvertResourcePath, KBErrorHandler.GetMethodName(), unauthedAccessEx.Message, LogEnabled.False,
                        new KBExceptionData("clientID", ClientID), new KBExceptionData("portalID", PortalID), new KBExceptionData("pathEnd", pathEnd), new KBExceptionData("path", path));
                    throw kbCustExp;
                }
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ConvertResourcePath, KBErrorHandler.GetMethodName(), GeneralResources.ConvertCDPathError,
                    new KBExceptionData("clientID", ClientID), new KBExceptionData("portalID", PortalID), new KBExceptionData("pathEnd", pathEnd), new KBExceptionData("path", path));
                throw kbCustExp;
            }

            return new ResponsivePortal.Models.FileResult(path);
        }
        public string GetDomainFromUrl(string sURL)
        {
            string[] hostParts = new System.Uri(sURL).Host.Split('.');
            string domain = String.Join(".", hostParts.Skip(Math.Max(0, hostParts.Length - 2)).Take(2));
            return domain;
        }
	}
}