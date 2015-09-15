using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Web;
using System.Web.Mvc;
using ResponsivePortal.Models;
using PortalAPI.Models.Widgets.Home;
using PortalAPI.Models;
using PortalAPI.Managers.Concrete;
using ResponsivePortal.Filters.MVC;
using PortalAPI.Models.Widgets;
using PortalAPI.Repositories.Concrete;
using PortalAPI.Repositories.Abstract;
using System.Web.Caching;
using NLog;
using ResponsivePortal.Filters;
namespace ResponsivePortal.Controllers
{
    [PortalConfigurationAction]
    [CustomErrorHandler]
    public class FavoritesController : Controller
    {
        private ArticleManager _articleManager;
        private UsersManager _usersManager;
        private int portalId;
        private int clientId;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Portal _portal;
        public HeaderViewModel headerVM;
        public Dictionary<string, string> resources = new Dictionary<string, string>();

        public FavoritesController(ArticleManager articleManager, UsersManager usersManager)
        {
            this._articleManager = articleManager;
            this._usersManager = usersManager;
        }

        public ActionResult ManageFavorites()
        {
            try
            {
                ReadDataFromRouteData();
                string homeText = Utilities.getModuleText(headerVM, "home");
                FavoritesViewModel favVM = new FavoritesViewModel();
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = Utilities.GetResourceText(resources, "MANAGEMYFAVORITES"), Value = "", Selected = true });
                favVM.BreadcrumbViewModel = BreadcrumbViewModel;
                favVM.portalId = portalId;
                favVM.clientId = clientId;
                favVM.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
                favVM.Resources = resources;
                return View("ManageFavs", favVM);
                //managemyfavorites
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }



        public void UpdateOrder(int rowindex, int rowid)
        {

            try
            {
                ReadDataFromRouteData();
                int updatesuccess = 0;
                //Re-Orders the  Favorites based on the Row Drag and Drop by user
                updatesuccess = this._articleManager.UpdateFavOrderonDnD(rowid, rowindex);

            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        public JsonResult GetFavJsondata(int page, int rows, string search, string sidx, string sord)
        {
            ReadDataFromRouteData();
            int maxCount = -1;
            if (rows > 0)
            {
                maxCount = rows;
            }
            FavoritesViewModel favVM = new FavoritesViewModel();
            favVM.portalId = portalId;
            favVM.clientId = clientId;
            favVM.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
            if (page > 0 && rows > 0)
            {
                favVM.ContentList = this._articleManager.GetFavorites(
                                                           n: -1,
                                                           dayCount: -1);
            }
            else
            {
                favVM.ContentList = this._articleManager.GetFavorites(
                                                           n: maxCount,
                                                           dayCount: -1);
            }
            var data = favVM.ContentList;
            if (page > 0 && rows > 0)
            {
                int count = page - 1;
                data = favVM.ContentList.Skip(rows * count).Take(rows).ToList();
            }
           
            var totalPages = 1;

            var jsondata = new
            {
                total = totalPages,
                page = maxCount,
                records = rows,

                rows = (
                           from m in data
                           select new
                           {
                               id = m.FavoriteId,
                               cell = new object[]{      m.ArticleBase.Id,
                                   m.ArticleBase.Title,
                                m.FavoriteOrder ,
                              String.Format("{0:MM/dd/yyyy}", m.LikeDate),
                              String.Format("{0:MM/dd/yyyy}", m.ViewDate),
                                m.ViewCount
                             }

                           }).ToArray()
            };
            return Json(jsondata, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetFavJsondata()
        {

            ReadDataFromRouteData();
            int maxCount=-1;
            FavoritesViewModel favVM = new FavoritesViewModel();
            favVM.portalId = portalId;
            favVM.clientId = clientId;
            favVM.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
            favVM.ContentList = this._articleManager.GetFavorites(
                                                       n: maxCount,
                                                       dayCount: -1);
            var data = favVM.ContentList;
            var totalPages = 1;

            var jsondata = new
            {
                total = totalPages,
                page = 1,
                records = 4,

                rows = (
                           from m in data
                           select new
                           {
                               id = m.FavoriteId,
                               cell = new object[]{      m.ArticleBase.Id,
                                   m.ArticleBase.Title,
                                m.FavoriteOrder ,
                              String.Format("{0:MM/dd/yyyy}", m.LikeDate),
                              String.Format("{0:MM/dd/yyyy}", m.ViewDate),
                                m.ViewCount
                             }

                           }).ToArray()
            };
            return Json(jsondata, JsonRequestBehavior.AllowGet);


        }

        public JsonResult DeleteFavorite(int? articleid)
        {
            try
            {
                int updatesuccess = 0;

                ReadDataFromRouteData();
                ///this calls the same method which is used from article details page to toggle as favorite/un favorite,which will update the likestatus
                updatesuccess = this._articleManager.SetFavoriteArticle(Convert.ToInt32(articleid), Convert.ToString(HttpContext.Session.SessionID));
                return GetFavJsondata();
            }

            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        private void ReadDataFromRouteData()
        {
            if (RouteData.Values["clientId"].GetType() == typeof(System.Int32))
            {
                clientId = (int)RouteData.Values["clientId"];
            }
            else
            {
                clientId = int.Parse((string)RouteData.Values["clientId"]);
            }

            if (RouteData.Values["portalId"].GetType() == typeof(System.Int32))
            {
                portalId = (int)RouteData.Values["portalId"];
            }
            else
            {
                portalId = int.Parse((string)RouteData.Values["portalId"]);
            }
            _portal = Session.GetPortalSessions().GetPortalSession(portalId,clientId).Portal;
            this._articleManager.Portal = _portal;
            this._articleManager.User = HttpContext.Session.GetUser(portalId);
            ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId,this._portal.PortalType, this._portal.Configuration,"favorite");
            headerVM = ((CommonViewModel)ViewData["CommonViewModel"]).HeaderViewModel;
            //resource object
            resources = Session.Resource(portalId, clientId, "favorites", _portal.Language.Name);
        }
        public JsonResult BindGridData(int page, int rows, string search, string sidx, string sord)
        {
            try
            {
                return GetFavJsondata(page,rows,search,sidx,sord);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }


        public JsonResult SetFavoriteOrder(int[] favoriteList)
        {
            try
            {
                ReadDataFromRouteData();
                _articleManager.SetFavoriteOrder(favoriteList);
                //return GetFavJsondata();
                return Json(new { Message = "Success - your favorites list has been updated" });
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
    }
}