using PortalAPI.Managers.Concrete;
using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using System.Reflection;
using PortalAPI.Repositories.Concrete;
using PortalAPI.Repositories.Abstract;
using Microsoft.Win32;
using System.IO;
using System.Xml.Linq;
using ResponsivePortal.Resources;
using System.Web.Caching;
using ResponsivePortal.Models;
using ResponsivePortal.Filters;
using KBCommon.KBException;
using NLog;
using ResponsivePortal.Controllers;

namespace ResponsivePortal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //set AntiForgeryConfig to use other (Name) ClaimType
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;

            InitializeAutofac();
        }

        protected void Session_Start()
        {
            //dictionary key=portalID

            Session.Add("PortalSessions", new PortalSessionDictionary());
            Session.Add("PortalResourceSessions", new PortalResourceDictionary());
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            string currentController = "";
            string currentAction = "";
            HttpContext httpContext = null;

            if (sender is MvcApplication)
            {
                httpContext = ((MvcApplication)sender).Context;
                var currentRouteData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(httpContext));

                if (currentRouteData != null)
                {
                    if (currentRouteData.Values["controller"] != null && !String.IsNullOrEmpty(currentRouteData.Values["controller"].ToString()))
                    {
                        currentController = currentRouteData.Values["controller"].ToString();
                    }

                    if (currentRouteData.Values["action"] != null && !String.IsNullOrEmpty(currentRouteData.Values["action"].ToString()))
                    {
                        currentAction = currentRouteData.Values["action"].ToString();
                    }
                }
            }

            var ex = Server.GetLastError();
            if (!(ex is KBCustomException))     // Unhandled exception expected
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ApplicationError, KBErrorHandler.GetMethodName(), GeneralResources.ApplicationExceptionError, LogEnabled.False);
                if (!string.IsNullOrEmpty(currentController) || !string.IsNullOrEmpty(currentAction))
                {
                    kbCustExp.SetDataKeyValues(new KBExceptionData("currentController", currentController), new KBExceptionData("currentAction", currentAction));
                }
                logger.ErrorException(GeneralResources.GeneralError, kbCustExp);
                kbCustExp.LoggerCalled = true;  // unusual scenario of needing to call logger after adding more data to exception object
            }
            else    // Do not expect to receive custom exception, defensive code
            {
                KBCustomException kbCustExp = ex as KBCustomException;
                if (!string.IsNullOrEmpty(currentController) || !string.IsNullOrEmpty(currentAction))
                {
                    kbCustExp.SetDataKeyValues(new KBExceptionData("currentController", currentController), new KBExceptionData("currentAction", currentAction));
                }
                KBCustomException.LogExceptionStackTrace(kbCustExp, KBErrorHandler.GetMethodName());
            }

            var controller = new ErrorController();
            var routeData = new RouteData();
            var action = "Index";

            // Note: we could expand this method to target specific ations depending on exception Http code
            //if (ex is HttpException)
            //{
            //    var httpEx = ex as HttpException;

            //    switch (httpEx.GetHttpCode())
            //    {
            //        case 404:
            //            action = "NotFound";
            //            break;

            //        // others if any
            //    }
            //}

            httpContext.ClearError();
            httpContext.Response.Clear();
            httpContext.Response.StatusCode = ex is HttpException ? ((HttpException)ex).GetHttpCode() : 500;
            httpContext.Response.TrySkipIisCustomErrors = true;

            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = action;

            Server.ClearError();

            controller.ViewData["ApplicationError"] = ex;
            //if (!string.IsNullOrEmpty(currentController) || !string.IsNullOrEmpty(currentAction))
            //{
            //    controller.ViewData.Model = new HandleErrorInfo(ex, currentController, currentAction);
            //}
            ((IController)controller).Execute(new RequestContext(new HttpContextWrapper(httpContext), routeData));
        }

        [CustomErrorHandler]
        private void InitializeAutofac()
        {
            try
            {
                // See https://code.google.com/p/autofac/wiki/WebApiIntegration for details.

                var builder = new ContainerBuilder();


                RegistryKey clientkey = Registry.LocalMachine;
                if (System.Environment.Is64BitOperatingSystem && System.Environment.Is64BitProcess)
                {
                    //logger.Warn("Detected a 64 bit system with 64 bit processes.  This configuration is not supported.");
                }
                else if (!System.Environment.Is64BitOperatingSystem)
                {
                    //logger.Warn("Detected a 32 bit system. This configuration is not supported.");
                }

                RegistryKey rootKey = clientkey.OpenSubKey(@"SOFTWARE\KnowledgeBase.Net\", true);
                string installPath = string.Empty;
                if (rootKey.GetValueNames().Contains("Program Files"))
                {
                    installPath = rootKey.GetValue("Program Files").ToString();
                }

                Application["KBInstallPath"] = installPath;

                string dataPath = string.Empty;
                if (rootKey.GetValueNames().Contains("Data Files"))
                {
                    dataPath = rootKey.GetValue("Data Files").ToString();
                }

                Application["KBDataPath"] = dataPath;

                IPortalRepository portalRep = new XmlPortalRepository(dataPath);
                Application.Add("PortalManager", new PortalManager(portalRep));
                PortalResource portalResourceRep = new PortalResource(dataPath);
                Application.Add("PortalResource", new PortalResourceManager(portalResourceRep));

                // Add your registrations
                var connectionStrings = new SqlNativeConnectionStrings(installPath);
                // data repository
                var datRepo = new DataConfigurationRepository(dataPath, installPath);

                builder.RegisterControllers(Assembly.GetExecutingAssembly());

                // Ideally we'd register all SqlServer*Respository types in one call.
                // But I'm not sure how to do it yet.
                //builder
                //    .RegisterAssemblyTypes(typeof(SqlServerUserRepository).Assembly)
                //    .Where(t => t.Name.StartsWith("SqlServer"))
                //    .Where(t => t.Name.EndsWith("Repository"))
                //    .As(t => t)
                //    .WithParameter("connectionStrings", connectionStrings);

                //builder
                //   .RegisterType<SqlNativeConnectionStrings>()
                //   .As<IConnectionStrings>()
                //   .WithParameter("stringsFilePath", "c:\\test");

                builder
                    .RegisterType<SqlServerArticleRepository>()
                    .As<IArticleRepository>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                        (paramInfo, componentContext) => connectionStrings)
                    .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                        (paramInfo, componentContext) => datRepo);
                builder
                    .RegisterType<SqlServerCategoryRepository>()
                    .As<ICategoryRepository>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                        (paramInfo, componentContext) => connectionStrings)
                    .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                        (paramInfo, componentContext) => datRepo);

                builder
                    .RegisterType<SqlServerSolutionFinderRepository>()
                    .As<ISolutionFinderRepository>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                        (paramInfo, componentContext) => connectionStrings)
                    .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                        (paramInfo, componentContext) => datRepo);

                builder.RegisterType<SearchRepository>()
                    .As<ISearchesRepository>()
                   .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                        (paramInfo, componentContext) => connectionStrings)
                    .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                        (paramInfo, componentContext) => datRepo);

                builder
                    .RegisterType<SqlServerUserRepository>()
                    .As<IUserRepository>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                        (paramInfo, componentContext) => connectionStrings)
                    .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                        (paramInfo, componentContext) => datRepo);

                builder
                   .RegisterType<TokenRepository>()
                   .As<ITokenRepository>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                       (paramInfo, componentContext) => connectionStrings)
                   .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                       (paramInfo, componentContext) => datRepo);

                 builder
                   .RegisterType<ActiveDirectoryRepository>()
                   .As<IActiveDirectoryRepository>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                       (paramInfo, componentContext) => connectionStrings)
                   .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                       (paramInfo, componentContext) => datRepo);

                builder
                   .RegisterType<SqlServerAdminRepository>()
                   .As<IAdminRepository>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(SqlNativeConnectionStrings),
                       (paramInfo, componentContext) => connectionStrings)
                   .WithParameter((paramInfo, componentContext) => paramInfo.ParameterType == typeof(DataConfigurationRepository),
                       (paramInfo, componentContext) => datRepo);

                builder
                    .RegisterType<ArticleManager>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(IArticleRepository),
                        (paramInfo, componentContext) => componentContext.Resolve<IArticleRepository>());
                builder
                   .RegisterType<SolutionFinderManager>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(ISolutionFinderRepository),
                       (paramInfo, componentContext) => componentContext.Resolve<ISolutionFinderRepository>());

                builder
                    .RegisterType<UsersManager>()
                    .WithParameter(
                        (paramInfo, componentContext) => paramInfo.ParameterType == typeof(IUserRepository),
                        (paramInfo, componentContext) => componentContext.Resolve<IUserRepository>());

                builder
                   .RegisterType<SearchesManager>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(ISearchesRepository),
                       (paramInfo, componentContext) => componentContext.Resolve<ISearchesRepository>());

                builder
                   .RegisterType<CategoryManager>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(ICategoryRepository),
                       (paramInfo, componentContext) => componentContext.Resolve<ICategoryRepository>());

                builder
                   .RegisterType<AdminManager>()
                   .WithParameter(
                       (paramInfo, componentContext) => paramInfo.ParameterType == typeof(IAdminRepository),
                       (paramInfo, componentContext) => componentContext.Resolve<IAdminRepository>());

                builder
                  .RegisterType<TokenManager>()
                  .WithParameter(
                      (paramInfo, componentContext) => paramInfo.ParameterType == typeof(ITokenRepository),
                      (paramInfo, componentContext) => componentContext.Resolve<ITokenRepository>());
                
                builder
                 .RegisterType<ActiveDirectoryManager>()
                 .WithParameter(
                     (paramInfo, componentContext) => paramInfo.ParameterType == typeof(IActiveDirectoryRepository),
                     (paramInfo, componentContext) => componentContext.Resolve<IActiveDirectoryRepository>());

                // We don't need to map UsersManager's ctor parameter; Autofac does that for us.
                //builder.RegisterType<UsersManager>();

                // Registers HTTP context abstraction classes including <see cref="HttpContextBase"/> and
                // <see cref="HttpSessionStateBase"/> for use by components that live in the HTTP request lifetime.
                // I'm not sure if we need it or not.
                builder.RegisterModule(new AutofacWebTypesModule());

                var container = builder.Build();
                container.ActivateGlimpse();

                // Set the dependency resolver for Web API.
                //var webApiResolver = new AutofacWebApiDependencyResolver(container);
                //GlobalConfiguration.Configuration.DependencyResolver = webApiResolver;

                // Set the dependency resolver for MVC.
                var mvcResolver = new AutofacDependencyResolver(container);
                DependencyResolver.SetResolver(mvcResolver);
            }
            catch (Exception ex)
            {
                throw ex;
                // todo
            }

        }
    }
    public static class ApplicationExtensions
    {
        public static Portal LoadPortalConfiguration(this HttpApplicationStateBase application, int clientId, int portalId, bool reload)
        {
            return ((PortalManager)application["PortalManager"]).LoadConfiguration(clientId, portalId, reload);
        }

        public static KBResource LoadPortalResource(this HttpApplicationStateBase application, int clientId, int portalId, string language, bool reload)
        {
            return ((PortalResourceManager)application["PortalResource"]).LoadConfiguration(clientId, portalId, language, reload);
        }

    }

    public static class SessionExtensions
    {
        public static PortalSessionDictionary GetPortalSessions(this HttpSessionStateBase session)
        {
            //dictionary key=portalID
            if ((PortalSessionDictionary)session["PortalSessions"] == null)
            {
                session.Add("PortalSessions", new PortalSessionDictionary());
            }
            return (PortalSessionDictionary)session["PortalSessions"];
        }
        public static PortalResourceDictionary GetPortalResourceSessions(this HttpSessionStateBase session)
        {
            //dictionary key=portalID
            if ((PortalResourceDictionary)session["PortalResourceSessions"] == null)
            {
                session.Add("PortalResourceSessions", new PortalResourceDictionary());
            }
            return (PortalResourceDictionary)session["PortalResourceSessions"];
        }
        public static Dictionary<string, string> Resource(this HttpSessionStateBase session, int portalId, int clientId, string entry, string language)
        {
            KBResource resource = ((PortalResourceDictionary)session["PortalResourceSessions"]).GetPortalResourceSession(portalId, clientId, language);
            if (resource.Resources.ContainsKey(entry.ToUpper())) return resource.Resources[entry.ToUpper()];
            return new Dictionary<string, string>();
        }

        public static User GetUser(this HttpSessionStateBase session, int portalId)
        {
            try
            {
                return (User)session["UserSession_" + portalId.ToString()];
            }
            catch (Exception) { return null; }
        }

        public static void RemoveUser(this HttpSessionStateBase session, int portalId)
        {
            session["UserSession_" + portalId.ToString()] = null;
        }
        public static DictionaryOfSearchFiltersAppliedColletion GetSearchFilters(this HttpSessionStateBase session, int portalId)
        {
            var allFilters = (PortalDictionaryOfSearchFiltersApplied)session["SearchFilersApplied"];
            DictionaryOfSearchFiltersAppliedColletion portalFilter = null;
            if (allFilters != null)
            {
                portalFilter = allFilters.FiltersApplied.ContainsKey(portalId) ? allFilters.FiltersApplied[portalId] : null;
            }
            if (portalFilter == null)
            {
                return null;
            }
            bool hasFilters = false;
            foreach (KeyValuePair<FilterType, List<SearchFiltersApplied>> list in portalFilter.FilterCollection)
            {
                if (list.Value.Count > 0)
                {
                    hasFilters = true;
                    break;
                }
            }

            portalFilter.hasFilters = hasFilters;
            return portalFilter;
        }

        public static PortalDictionaryOfSearchFiltersApplied GetAllPortalFilters(this HttpSessionStateBase session)
        {
            return (PortalDictionaryOfSearchFiltersApplied)session["SearchFilersApplied"];
        }
        public static void RemovelAllSearchFilters(this HttpSessionStateBase session, int portalId)
        {

            var allFilters = (PortalDictionaryOfSearchFiltersApplied)session["SearchFilersApplied"];
            if (allFilters != null && allFilters.FiltersApplied.ContainsKey(portalId))
            {
                allFilters.FiltersApplied[portalId] = null;
                session["SearchFilersApplied"] = allFilters;
            }
        }

        public static DictionaryOfcategoryArticleViewModel GetcategoryBrowseResult(this HttpSessionStateBase session)
        {
            try
            {
                return (DictionaryOfcategoryArticleViewModel)session["DictionaryOfcategoryArticle"];

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static DictionaryOfcategoryApplied GetListOfcategoryApplied(this HttpSessionStateBase session)
        {
            try
            {
                return (DictionaryOfcategoryApplied)session["DictionaryOfcategoryApplied"];
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static void RemovelAllSearchResults(this HttpSessionStateBase session, int portalId)
        {

            var dictOfSearchRes = (DicttonaryOfSearchResult)session["LastSearchResult"];
            if (dictOfSearchRes != null && dictOfSearchRes.DictOfSearchResult.ContainsKey(portalId))
            {
                dictOfSearchRes.DictOfSearchResult[portalId] = null;
                session["LastSearchResult"] = dictOfSearchRes;
            }
        }
        public static SearchResult GetLastSearchResult(this HttpSessionStateBase session, int portalId)
        {
            var dictOfSearchRes = (DicttonaryOfSearchResult)session["LastSearchResult"];
            if (dictOfSearchRes != null && dictOfSearchRes.DictOfSearchResult.ContainsKey(portalId))
            {
                return (SearchResult)dictOfSearchRes.DictOfSearchResult[portalId];
            }
            return null;
        }

        public static void StoreLastSearchResult(this HttpSessionStateBase session, SearchResult result, int portalId)
        {
            var dictOfSearchRes = (DicttonaryOfSearchResult)session["LastSearchResult"];
            if (dictOfSearchRes != null)
            {
                dictOfSearchRes.DictOfSearchResult[portalId] = result;
                session["LastSearchResult"] = dictOfSearchRes;
            }

            else
            {
                dictOfSearchRes = new DicttonaryOfSearchResult();
                var DictOfSearchResult = new Dictionary<int, SearchResult>();
                dictOfSearchRes.DictOfSearchResult = DictOfSearchResult;
                dictOfSearchRes.DictOfSearchResult[portalId] = result;
                session["LastSearchResult"] = dictOfSearchRes;
            }
        }
        public static void LogPortalVisit(int userid, int portalId, int clientId, int newlog, string url)
        {
            string ipv4Address = "";

            var connectionStrings = new SqlNativeConnectionStrings(HttpContext.Current.Application["KBInstallPath"].ToString());
            var datRepo = new DataConfigurationRepository(HttpContext.Current.Application["KBDataPath"].ToString(), HttpContext.Current.Application["KBInstallPath"].ToString());
            SqlServerPortalLogRepository portalLogRep = new SqlServerPortalLogRepository(connectionStrings, datRepo);
            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                ipv4Address = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(new char[] { ',' }).FirstOrDefault();
            }
            if (ipv4Address == "")
            {
                ipv4Address = HttpContext.Current.Request.UserHostAddress;
            }
            portalLogRep.PortalVisitAsync(portalId, clientId, userid, HttpContext.Current.Session.SessionID, ipv4Address, newlog, url);
        }

        public static void UpdateLogoff(this HttpSessionStateBase session, int portalId, int clientId)
        {

            User user = (User)session["UserSession_" + portalId.ToString()];             
            int userid = user.UserId;
            string ipv4Address = "";
            var connectionStrings = new SqlNativeConnectionStrings(HttpContext.Current.Application["KBInstallPath"].ToString());
            var datRepo = new DataConfigurationRepository(HttpContext.Current.Application["KBDataPath"].ToString(), HttpContext.Current.Application["KBInstallPath"].ToString());
            SqlServerPortalLogRepository portalLogRep = new SqlServerPortalLogRepository(connectionStrings, datRepo);
            if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                ipv4Address = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(new char[] { ',' }).FirstOrDefault();
            }
            if (ipv4Address == "")
            {
                ipv4Address = HttpContext.Current.Request.UserHostAddress;
            }

            portalLogRep.Logoff(portalId, clientId, userid, HttpContext.Current.Session.SessionID, ipv4Address);
        }
    }


    public class PortalSessionDictionary
    {
        private Dictionary<int, PortalSession> _portalSessions;
        public PortalSessionDictionary()
        {
            _portalSessions = new Dictionary<int, PortalSession>();
        }
        public PortalSession GetPortalSession(int portalId, int clientId)
        {
            if (_portalSessions.ContainsKey(portalId))
            {
                return this._portalSessions[portalId];
            }
            else
            {
                Portal portal = ((PortalManager)HttpContext.Current.Application["PortalManager"]).LoadConfiguration(clientId, portalId);
                PortalSession portalSession = new PortalSession() { Portal = portal };
                AddPortalSession(portalSession);
                CacheDependency cd = new CacheDependency(HttpContext.Current.Application["KBDataPath"] + @"/knowledgebase/PortalConfiguration/" + clientId + "/" + portalId + "/configuration/portal-config.xml");
                HttpContext.Current.Cache.Insert(portalId.ToString(), "portal", cd, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration);
                return portalSession;
            }
        }
        public void AddPortalSession(PortalSession portalSession)
        {
            if (portalSession == null) throw new ArgumentNullException("portalsession null");
            _portalSessions.Add(portalSession.Portal.PortalId, portalSession);

            if(HttpContext.Current.Session["domain_" + portalSession.Portal.ClientId.ToString() + "_" + portalSession.Portal.PortalId.ToString()]==null)
            {
                HttpContext.Current.Session.Add("domain_" + portalSession.Portal.ClientId.ToString() + "_" + portalSession.Portal.PortalId.ToString(),GetDomainFromUrl(HttpContext.Current.Request.Url.ToString()));
            }
            
        }
        public bool HasPortalSession(int portalId)
        {
            return _portalSessions.ContainsKey(portalId);
        }
        public int Count
        {
            get { return _portalSessions.Count(); }
        }
        public string GetDomainFromUrl(string sURL)
        {
            string[] hostParts = new System.Uri(sURL).Host.Split('.');
            string domain = String.Join(".", hostParts.Skip(Math.Max(0, hostParts.Length - 2)).Take(2));
            return domain;
        }
    }

    public class PortalResourceDictionary
    {
        public Dictionary<int, KBResource> _portalResourceSessions;
        public PortalResourceDictionary()
        {
            _portalResourceSessions = new Dictionary<int, KBResource>();
        }
        public KBResource GetPortalResourceSession(int portalId, int clientId, string language)
        {
            if (_portalResourceSessions.ContainsKey(portalId))
            {
                return this._portalResourceSessions[portalId];
            }
            else
            {
                KBResource portalResource = ((PortalResourceManager)HttpContext.Current.Application["PortalResource"]).LoadConfiguration(clientId, portalId, language, true);
                AddPortalResourceSession(portalResource);
                return portalResource;
            }
        }
        public void AddPortalResourceSession(KBResource portalResource)
        {
            if (portalResource == null) throw new ArgumentNullException("portalResourcesession null");
            _portalResourceSessions.Add(portalResource.PortalId, portalResource);
        }
        public void RemovePortalResourceSession(int portalId)
        {
            if (HasResourceSession(portalId)) _portalResourceSessions.Remove(portalId);
        }
        public bool HasResourceSession(int portalId)
        {
            return _portalResourceSessions.ContainsKey(portalId);
        }
        public int Count
        {
            get { return _portalResourceSessions.Count(); }
        }
    }
}
