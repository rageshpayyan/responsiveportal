using PortalAPI.Models;
using PortalAPI.Models.Widgets.Home;
using ResponsivePortal.Controllers;
using ResponsivePortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using ResponsivePortal.Resources;
using System.IO;
using NLog;
using System.Web.Caching;
using KBCommon.KBException;


namespace ResponsivePortal.Filters.MVC
{
    [CustomErrorHandler]
    public class PortalConfigurationActionAttribute : FilterAttribute, IActionFilter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //if (filterContext.RouteData.GetRequiredString("action").ToLowerInvariant() != "index")
            //{
            //    return;
            //}

            //string path = GetConfigXmlPath(filterContext.RouteData);
            //PortalConfigurationBuilder builder = filterContext.HttpContext.Session.GetPortalConfigurationBuilder();
            //builder.LoadPortalConfigurationManager(_clientId, _portalId, path);    

            //1. No portal Sessions
            //2. Different Portal Sessions
            //3. dictionary is empty
            int portalId = GetPortalId(filterContext.RouteData);
            int clientId = GetClientId(filterContext.RouteData);
            var portalSessions = filterContext.HttpContext.Session.GetPortalSessions();
            var portalResourceSessions = filterContext.HttpContext.Session.GetPortalResourceSessions();
            if (!portalSessions.HasPortalSession(portalId))
            {
                Portal portal = null;
                try
                {
                    portal = filterContext.HttpContext.Application.LoadPortalConfiguration(clientId, portalId, true);

                    if (portal == null)
                    {
                        KBCustomException kbCustUserExp = KBCustomException.ProcessException(null, KBOp.LoadConfigFile, KBErrorHandler.GetMethodName(), GeneralResources.IOError + ". " + GeneralResources.ConfigPortalAdminError, LogEnabled.False,
                            new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId));
                        kbCustUserExp.SetUrlRouteKeyValues(new KBUrlRoute("Controller", "Admin"), new KBUrlRoute("Action", "Index"), new KBUrlRoute("portalId", portalId));
                        throw kbCustUserExp;
                    }
                    string url = HttpContext.Current.Request.Url.ToString();

                    SessionExtensions.LogPortalVisit(0, portalId, clientId, 1, url);

                }
                catch (Exception ex)
                {
                    KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadConfigFile, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError,
                        new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId));
                    if (ex.InnerException is FileNotFoundException)
                    {
                        // Build new exception with user friendly error message
                        KBCustomException kbCustUserExp = KBCustomException.ProcessException(ex.InnerException, KBOp.LoadConfigFile, KBErrorHandler.GetMethodName(), GeneralResources.IOError + ". " + GeneralResources.ConfigPortalAdminError,
                            new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId));
                        throw kbCustUserExp;
                    }
                    else
                    {
                        throw kbCustExp;
                    }
                }

                PortalSession portalSession = new PortalSession() { Portal = portal };
                CacheDependency cd = new CacheDependency(filterContext.HttpContext.Application["KBDataPath"] + @"/knowledgebase/PortalConfiguration/" + clientId + "/" + portalId + "/configuration/portal-config.xml");
                filterContext.HttpContext.Cache.Insert(portalId.ToString(), "portal", cd, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
                portalSessions.AddPortalSession(portalSession);
            }
            // update portal session based on cache depedency
            if (filterContext.HttpContext.Cache[portalId.ToString()] == null)
            {
                Portal portal = filterContext.HttpContext.Application.LoadPortalConfiguration(clientId, portalId, true);
                PortalSession portalSession = portalSessions.GetPortalSession(portalId, clientId);
                portalSession.Portal = portal;
                CacheDependency cd = new CacheDependency(filterContext.HttpContext.Application["KBDataPath"] + @"/knowledgebase/PortalConfiguration/" + clientId + "/" + portalId + "/configuration/portal-config.xml");
                filterContext.HttpContext.Cache.Insert(portalId.ToString(), "portal", cd);
            }

            Portal lportal = portalSessions.GetPortalSession(portalId, clientId).Portal;

            //check iprestriction
            if (!CheckPortalIpWithinIpRestrictions(filterContext, lportal))
            {
                // Client ip is restricted, redirect to error page.
                KBCustomException kbCustExp = KBCustomException.ProcessException(null, KBOp.CheckSourceIp, KBErrorHandler.GetMethodName(), GeneralResources.IPAddressBlockedError,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId));
                throw kbCustExp;
            }
            filterContext.HttpContext.Session["PortalType"] = lportal.PortalType;

            if (!portalResourceSessions.HasResourceSession(portalId))
            {
                var resource = filterContext.HttpContext.Application.LoadPortalResource(clientId, portalId, lportal.Language.Name, true);
                CacheDependency cdlang = new CacheDependency(filterContext.HttpContext.Application["KBDataPath"] + @"/knowledgebase/PortalConfiguration/" + clientId + "/" + portalId + "/languages/" + Utilities.GetResourceFileName(lportal.Language.Name));
                filterContext.HttpContext.Cache.Insert(portalId.ToString() + lportal.Language.Name.ToString(), "portallanguage", cdlang, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
                portalResourceSessions.AddPortalResourceSession(resource);
            }

            // update portalresource session based on cache depedency
            if (filterContext.HttpContext.Cache[portalId.ToString() + lportal.Language.Name.ToString()] == null)
            {
                portalResourceSessions.RemovePortalResourceSession(portalId);
                var resource = filterContext.HttpContext.Application.LoadPortalResource(clientId, portalId, lportal.Language.Name, true);
                portalResourceSessions.AddPortalResourceSession(resource);
                CacheDependency cdlang = new CacheDependency(filterContext.HttpContext.Application["KBDataPath"] + @"/knowledgebase/PortalConfiguration/" + clientId + "/" + portalId + "/languages/" + Utilities.GetResourceFileName(lportal.Language.Name));
                filterContext.HttpContext.Cache.Insert(portalId.ToString() + lportal.Language.Name.ToString(), "portallanguage", cdlang, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
            }

            if (lportal.PortalType != PortalType.Open)
            {
                var isAuthenticated = System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
                if (!isAuthenticated)
                {
                    filterContext.HttpContext.Session.RemoveUser(portalId);
                }
                var user = filterContext.HttpContext.Session.GetUser(portalId);
                //Get AD enabled from shared configuration file.
                var ADSIEnabled = false;
                PortalAPI.Repositories.Concrete.DataConfigurationRepository dataConfigurationRepository = new PortalAPI.Repositories.Concrete.DataConfigurationRepository(HttpContext.Current.Application["KBDataPath"].ToString(), HttpContext.Current.Application["KBInstallPath"].ToString());

                if (dataConfigurationRepository != null)
                { ADSIEnabled = dataConfigurationRepository.ADEnabled; }

                if (user == null)
                {
                    if ((ADSIEnabled) && (HttpContext.Current.Request.LogonUserIdentity.IsAuthenticated))
                    {
                        string uname = HttpContext.Current.User.Identity.Name.ToLower();
                        if (uname == string.Empty) uname = Environment.UserName.ToLower();
                        logger.Info("AD user name:" + uname);
                        if (uname.Contains(@"\"))
                        {
                            string[] unameArray = uname.Split('\\');
                            if (unameArray.Length == 2)
                            {
                                string username = unameArray[1];
                                // redirect to AD login
                                var routeDict = new RouteValueDictionary();
                                routeDict.Add("Controller", "Account");
                                routeDict.Add("Action", "ADLogin");
                                routeDict.Add("portalId", portalId);
                                routeDict.Add("username", username);
                                filterContext.Result = new RedirectToRouteResult(routeDict);
                            }
                        }
                    }
                    else
                    {
                        // redirect to login page
                        var routeDict = new RouteValueDictionary();
                        routeDict.Add("Controller", "Account");
                        routeDict.Add("Action", "Login");
                        routeDict.Add("portalId", portalId);
                        filterContext.Result = new RedirectToRouteResult(routeDict);
                    }
                }
                else
                {
                    if (!user.Profile.AuthorizedPortals.Contains(lportal.PortalId))
                    {
                        KBCustomException kbCustUserExp = KBCustomException.ProcessException(null, KBOp.GetUserAuth, KBErrorHandler.GetMethodName(), GeneralResources.UserNotAuthedError, LogEnabled.False,
                            new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId));
                        kbCustUserExp.SetUrlRouteKeyValues(new KBUrlRoute("Controller", "Account"), new KBUrlRoute("Action", "LogOffNotAuthorized"));
                        logger.ErrorException(kbCustUserExp.DataSummary(), kbCustUserExp);
                        kbCustUserExp.LoggerCalled = true;  // unusual scenario of needing to call logger after adding more data to exception object
                        throw kbCustUserExp;
                    }
                }
            }
        }

        private bool CheckPortalIpWithinIpRestrictions(ActionExecutingContext filterContext, Portal portal)
        {
            if (portal.IpRestrictions.Count == 0) { return true; }
            string ipv4Address = "";
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]))
            {
                ipv4Address = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(new char[] { ',' }).FirstOrDefault();
                string[] userIP = ipv4Address.Split('.');
                int userIPInt = (Convert.ToInt32(userIP[0]) * 256 * 256 * 256) + (Convert.ToInt32(userIP[1]) * 256 * 256) +
                    (Convert.ToInt32(userIP[2]) * 256) + Convert.ToInt32(userIP[3]);
                foreach (IpRestriction iprestrict in portal.IpRestrictions)
                {
                    string[] listFromIP = iprestrict.From.Split('.');
                    string[] listToIP = iprestrict.To.Split('.');
                    int beginIP = (Convert.ToInt32(listFromIP[0]) * 256 * 256 * 256) + (Convert.ToInt32(listFromIP[1]) * 256 * 256) + (Convert.ToInt32(listFromIP[2]) * 256) + Convert.ToInt32(listFromIP[3]);
                    int endIP = (Convert.ToInt32(listToIP[0]) * 256 * 256 * 256) + (Convert.ToInt32(listToIP[1]) * 256 * 256) + (Convert.ToInt32(listToIP[2]) * 256) + Convert.ToInt32(listToIP[3]);
                    if ((userIPInt >= beginIP) && (userIPInt <= endIP))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }



        private FooterViewModel CreateFooterViewModel()
        {
            FooterViewModel footerViewModel = new FooterViewModel();
            footerViewModel.Content = "Footer";
            return footerViewModel;
        }

        private int GetClientId(RouteData routeData)
        {
            if (routeData.Values["clientId"].GetType() == typeof(System.Int32))
                return (int)routeData.Values["clientId"];

            return int.Parse((string)routeData.Values["clientId"]);
        }

        private int GetPortalId(RouteData routeData)
        {

            if (routeData.Values["portalId"].GetType() == typeof(System.Int32))
                return (int)routeData.Values["portalId"];

            return int.Parse((string)routeData.Values["portalId"]);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            // Do nothing.
        }
    }
}