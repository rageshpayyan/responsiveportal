using ResponsivePortal.Resources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ResponsivePortal.Controllers
{
    public class ErrorController :Controller
    {
        public ActionResult Index()
        {
            Exception ex = null;

            bool standardErrorMsgEnabled = bool.TryParse(ConfigurationManager.AppSettings["StandardErrorMsgEnabled"], out standardErrorMsgEnabled) ? standardErrorMsgEnabled : false;
            string message = string.Empty;
            if (standardErrorMsgEnabled)
            {
                ViewData.Add("ErrorMsg", GeneralResources.StandardErrorMessage);
            }
            else
            {
                if (ViewData["ApplicationError"] != null)
                {
                    ex = (Exception)ViewData["ApplicationError"];       // Note: could check if ex is HttpException and display different errors depending on specific exception received.
                    ViewData["ApplicationError"] = ex.Message;
                }
                if (!string.IsNullOrEmpty((string)TempData["ErrorMsg"]))
                {
                    message = (string)TempData["ErrorMsg"];
                    ViewData.Add("ErrorMsg", message);
                }
                if (!string.IsNullOrEmpty((string)TempData["ConfigErrorMsg"]))
                {
                    message = (string)TempData["ConfigErrorMsg"];
                    ViewData.Add("ConfigErrorMsg", message);
                }
                if (!string.IsNullOrEmpty((string)TempData["IpRestrictionErrorMsg"]))
                {
                    message = (string)TempData["IpRestrictionErrorMsg"];
                    ViewData.Add("IpRestrictionErrorMsg", message);
                }
            }
            try
            {
                Response.Cookies.Clear();
                //Response.StatusCode = 404;
                Response.Headers.Clear();
            }
            catch
            { }
            return View("Index");
        }
    }
}