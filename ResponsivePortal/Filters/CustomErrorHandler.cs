using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NLog;
using System.Web.Mvc;
using System.Web.Routing;
using KBCommon.KBException;

namespace ResponsivePortal.Filters
{
    public class CustomErrorHandler : FilterAttribute, IExceptionFilter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int portalID;
        private int clientID;
        private Dictionary<string, string> Resources = new Dictionary<string, string>();
        public void OnException(ExceptionContext filterContext)
        {
            //if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
            //    return;

            var routeDict = new RouteValueDictionary();
            portalID = GetPortalId(filterContext.RouteData);
            clientID = GetClientId(filterContext.RouteData);
            if (filterContext.Exception.Message.ToLower().Contains("requestverificationtoken"))
            {
                logger.ErrorException(filterContext.Exception.Message, filterContext.Exception);
                HttpContext.Current.Response.StatusCode = 403;
                HttpContext.Current.Response.End();
            }
            if (filterContext.Exception is KBCustomException)
            {
                KBCustomException kbCustExp = filterContext.Exception as KBCustomException;
                // Custom error routing behavior
                foreach (var urlRoute in kbCustExp.UrlRoutes)
                {
                    routeDict.Add(urlRoute.Key, urlRoute.Value);
                }
                if (!kbCustExp.LoggerCalled)
                {
                    logger.ErrorException(kbCustExp.Summary("Error"), filterContext.Exception);
                }
            }
            else
            {
                logger.ErrorException(filterContext.Exception.Message, filterContext.Exception);
            }

            if (routeDict.Count == 0)
            {
                // Default error routing behavior
                routeDict.Add("Controller", "Error");
                routeDict.Add("Action", "Index");
            }
            //filterContext.Controller.TempData["ErrorMsg"] = filterContext.Exception.Message;
            filterContext.Controller.TempData["ErrorMsg"] = processExceptionMessage(filterContext.Exception.Message.ToString(), filterContext.HttpContext);  
            filterContext.Result = new RedirectToRouteResult(routeDict);
            filterContext.ExceptionHandled = true;          // Stop any other exception handlers from running
            filterContext.HttpContext.Response.Clear();     // CLear out anything already in the response
        }
        private string processExceptionMessage(string Message, HttpContextBase context)
        {
            string processMessage = Message;
            string languageName;
            try
            {
                languageName = context.Session.GetPortalSessions().GetPortalSession(portalID, clientID).Portal.Language.Name.ToString();
                Resources = context.Session.Resource(portalID, clientID, "search", languageName);
                if (Message.IndexOf("Invalid search text supplied, please change text and resubmit") > -1)
                {
                    processMessage = Utilities.GetResourceText(Resources, "IGNOREWORDMSG", "The query contained only ignored words. Please use your back button to search again.");
                }
            }
            catch
            {
            }
            return processMessage;
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
    }
}