using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;

namespace ResponsivePortal.Filters.MVC
{
    public class AuthorizeResourceAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
                return false;
            }

            String UrlReferrer = HttpContext.Current.Request.UrlReferrer != null ? HttpContext.Current.Request.UrlReferrer.ToString() : "";

            var clientId = HttpContext.Current.Request.RequestContext.RouteData.Values["clientId"];
            var portalId = HttpContext.Current.Request.RequestContext.RouteData.Values["portalId"];
            var sessionDomainName = HttpContext.Current.Session["domain_" + clientId.ToString() + "_" + portalId.ToString()] != null ? HttpContext.Current.Session["domain_" + clientId.ToString() + "_" + portalId.ToString()] : "";
            string refDomainName = UrlReferrer != "" ? GetDomainFromUrl(UrlReferrer) : "";


            if (sessionDomainName == "" || sessionDomainName.ToString().ToLower() != refDomainName.ToString().ToLower())
            {
                return false;
            }
            return true;
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            UrlHelper urlHelper = new UrlHelper(filterContext.RequestContext);

            filterContext.Controller.TempData["ErrorMsg"] = "Unauthorized Access."; //You do not have sufficient privileges for this operation
            filterContext.Result = new RedirectResult(urlHelper.Action("Index", "Error"));
        }
        public string GetDomainFromUrl(string sURL)
        {
            string[] hostParts = new System.Uri(sURL).Host.Split('.');
            string domain = String.Join(".", hostParts.Skip(Math.Max(0, hostParts.Length - 2)).Take(2));
            return domain;
        }
    }
}