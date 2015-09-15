using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ResponsivePortal
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // clientId and portalId are required
            //
            routes.MapRoute(
             name: "Content",
             url: "Content/{action}/{clientId}/{portalId}/{fileName}",
             defaults: new
             {
                 controller = "Content",
                 action = "Index",
                 clientId = Settings.DEFAULT_CLIENTID,
                 portalId = Settings.DEFAULT_PORTALID,
                 fileName = ""
             }
           );
            routes.MapRoute(
           "ArticleImage",
           "Content/{action}/{clientId}/{kbId}/{articleId}/{fileName}",
           new
            {
                controller = "Content",
                action = "ArticleImage",
                clientId = Settings.DEFAULT_CLIENTID,
                kbId = Settings.DEFAULT_KBID,
                articleId = 0,
                fileName = ""
            }
          );
            routes.MapRoute(
          "ArticleFiles",
          "Content/{action}/{clientId}/{kbId}/{articleId}/{fileName}",
          new
          {
              controller = "Content",
              action = "ArticleFiles",
              clientId = Settings.DEFAULT_CLIENTID,
              kbId = Settings.DEFAULT_KBID,
              articleId = 0,
              fileName = ""
          }
         );
            routes.MapRoute(
                "CD",
                "cd/{ClientID}/{PortalID}/{*PathEnd}",
                new { 
                    controller = "CD", 
                    action = "Index", 
                    ClientID = Settings.DEFAULT_CLIENTID,
                    PortalID = Settings.DEFAULT_PORTALID, 
                    PathEnd = "" 
                }
            );
            routes.MapRoute(
                "PF",
                "pf/{ClientID}/{PortalID}/{*PathEnd}",
                new { 
                    controller = "PF", 
                    action = "Index",
                    ClientID = Settings.DEFAULT_CLIENTID,
                    PortalID = Settings.DEFAULT_PORTALID, 
                    PathEnd = "" 
                }
            );
            routes.MapRoute(
                name: "Portal",
                url: "{controller}/{action}/{clientId}/{portalId}/{id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    clientId = Settings.DEFAULT_CLIENTID,
                    portalId = Settings.DEFAULT_PORTALID,
                    id = UrlParameter.Optional
                }
            );
            routes.MapRoute(
               name: "Admin",
               url: "Admin/{action}/{clientId}/{portalId}/{id}",
               defaults: new
               {
                   controller = "Admin",
                   action = "Index",
                   clientId = Settings.DEFAULT_CLIENTID,
                   portalId = Settings.DEFAULT_PORTALID,
                   id = UrlParameter.Optional
               }
           );
            routes.MapRoute(
            name: "Browse",
            url: "Browse/{action}/{clientId}/{portalId}/{catId}",
            defaults: new
            {
                controller = "Browse",
                action = "categoryBrowse",
                clientId = Settings.DEFAULT_CLIENTID,
                portalId = Settings.DEFAULT_PORTALID,
                pcatId = UrlParameter.Optional,
                catId = UrlParameter.Optional,
                title = UrlParameter.Optional,
                searchText = UrlParameter.Optional,
                pTitle = UrlParameter.Optional,
                page = UrlParameter.Optional,
                paging = UrlParameter.Optional,
                removeFilter = UrlParameter.Optional

            }
        );
            routes.MapRoute(
                name: "Search",
                url: "Search/{action}/{clientId}/{portalId}/{searchtext}/{groupid}/{catid}/{attid}",
                defaults: new
                {
                    controller = "Search",
                    action = "GetSearch",
                    clientId = Settings.DEFAULT_CLIENTID,
                    portalId = Settings.DEFAULT_PORTALID,
                    searchtext = Settings.DEFAULT_SEARCHTEXT,
                    groupid = UrlParameter.Optional,
                    catid = UrlParameter.Optional,
                    attid = UrlParameter.Optional,
                    title = UrlParameter.Optional,
                    contentTypeId = UrlParameter.Optional,
                    format = UrlParameter.Optional,
                    page = UrlParameter.Optional,
                    pageNavigate = UrlParameter.Optional,
                    filterUpdate = UrlParameter.Optional,
                    spellCheck = UrlParameter.Optional
                }
            );
            routes.MapRoute(
               name: "Login",
               url: "{controller}/{action}/{clientId}/{portalId}",
               defaults: new
               {
                   controller = "Account",
                   action = "Login",
                   clientId = Settings.DEFAULT_CLIENTID,
                   portalId = Settings.DEFAULT_PORTALID
               }
            );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}
