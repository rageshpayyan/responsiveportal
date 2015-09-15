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
using KBCommon.KBException;
using ResponsivePortal.Resources;
using System.Web.Routing;

namespace ResponsivePortal.Controllers
{
    [CustomErrorHandler]
    [PortalConfigurationAction]
    public class HomeController : Controller
    {
        private ArticleManager _articleManager;
        private CategoryManager _categoryManager;
        private UsersManager _usersManager;
        private SearchesManager _searchManager;
        private Portal portal;
        private Cache _Cache;
        private int portalId;
        private int clientId;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public Dictionary<string, string> Resources = new Dictionary<string, string>();
        public Dictionary<string, string> CommonResources = new Dictionary<string, string>();
        /// <summary>
        ///   Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="articleManager">The article manager.</param>
        public HomeController(ArticleManager articleManager, CategoryManager categoryManager, UsersManager usersManager, SearchesManager searchesManager)
        {
            this._articleManager = articleManager;
            this._categoryManager = categoryManager;
            this._usersManager = usersManager;
            this._searchManager = searchesManager;
        }

        public ActionResult Index()
        {
            try
            {
                //read portal id and clientid from route data
                ReadDataFromRouteData();
                Session.RemovelAllSearchFilters(portalId);
                Session.RemovelAllSearchResults(portalId);
                HomeViewModel homeViewModel = GetHomeViewModel(portal);
                if (homeViewModel == null)
                {
                    // if home module is disable redirect to article tab as article is always be there.
                    HomeModule home = (HomeModule)portal.Configuration.HomeModule;
                    if (home == null)
                    {
                        return RedirectToAction("GetSearch" + "/" + clientId + "/" + portalId, "Search", new { text = "*" });
                    }
                    else
                    {
                        KBCustomException kbCustExp = KBCustomException.ProcessException(null, KBOp.LoadHomePage, KBErrorHandler.GetMethodName(), GeneralResources.HomeModuleNotFoundError, LogEnabled.False);
                        throw kbCustExp;
                    }
                }
                ViewData["MainLayoutViewModel"] = homeViewModel;
                Session["MainLayoutViewModel"] = homeViewModel;
                _Cache = HttpContext.Cache;
                // Get community Clues
                if (_Cache["CommClues_" + portalId.ToString()] == null)
                {
                    _Cache["CommClues_" + portalId.ToString()] = 0;
                    Dictionary<string, int> CommunityClues = new Dictionary<string, int>();
                    CommunityClues = _searchManager.GetCommunityClues();
                    if (CommunityClues != null && CommunityClues.Count > 0)
                    {
                        CacheDependency cd = new CacheDependency(_searchManager.GetAutocompleteFilePath());
                        _Cache.Insert("CommClues_" + portalId.ToString(), CommunityClues, cd);
                    }
                    else _Cache.Remove("CommClues_" + portalId.ToString());
                }
                
                return View(homeViewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.HomePage, KBErrorHandler.GetMethodName(), GeneralResources.HomePageError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        /// <summary>
        /// Fills the homeview model and returns the view model
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private HomeViewModel GetHomeViewModel(Portal portal)
        {
            try
            {
                HomeModule home = (HomeModule)portal.Configuration.HomeModule;
                if (null != home)
                {
                    HomeViewModel homeViewModel = new HomeViewModel();
                    homeViewModel.Resources = Resources;
                    homeViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources,"SESSIONTIMEOUTWARNING");
                    homeViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources,"SESSIONTIMEDOUT");
                    homeViewModel.portalId = portalId;
                    homeViewModel.clientId = clientId;
                    SimpleSearchWidget simpleSearchWidget = home.GetWidget<SimpleSearchWidget>();
                    AttributesWidget attributesWidget = home.GetWidget<AttributesWidget>();
                    CategoriesWidget categoriesWidget = home.GetWidget<CategoriesWidget>();
                    HotTopicsWidget hotTopicsWidget = home.GetWidget<HotTopicsWidget>();
                    FavoritesWidget favoritesWidget = home.GetWidget<FavoritesWidget>();
                    List<ArticlesFromCategoryWidget> articlesFromCategoriesWidgets = home.GetWidgets<ArticlesFromCategoryWidget>();
                    List<ArticlesFromCategoryWidget> orderedArticlesFromCategoriesWidgets = articlesFromCategoriesWidgets.OrderBy(x => x.Id).ToList();
                    List<CustomMessageWidget> customessageWidgets = home.GetWidgets<CustomMessageWidget>();

                    homeViewModel.SearchViewModel = CreateSearchViewModel(simpleSearchWidget);
                    homeViewModel.AttributesViewModel = CreateAttributesViewModel(attributesWidget);
                    homeViewModel.CategoriesViewModel = CreateCategoriesViewModel(categoriesWidget);
                    homeViewModel.HotTopicsViewModel = CreateHotTopicsViewModel(hotTopicsWidget);
                    homeViewModel.FavoritesViewModel = CreateFavoritesViewModel(favoritesWidget);
                    homeViewModel.ArticlesFromCategoryViewModelList = CreateArticlesFromCategoryViewModelList(orderedArticlesFromCategoriesWidgets);
                    homeViewModel.CustomMessageViewModelList = CreateCustomMessageViewModelList(customessageWidgets);
                    return homeViewModel;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetViewModel, KBErrorHandler.GetMethodName(), GeneralResources.HomePageError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private SearchViewModel CreateSearchViewModel(SimpleSearchWidget simpleSearchWidget)
        {
            try
            {
                SearchViewModel searchViewModel = new SearchViewModel();
                searchViewModel.clientId = portal.ClientId;
                searchViewModel.portalId = portal.PortalId;
                if (simpleSearchWidget != null)
                {
                    searchViewModel.Title = Utilities.GetResourceText(Resources, "SEARCHWIDGET_TITLE");
                    searchViewModel.SearchLabel = Utilities.GetResourceText(Resources, "SEARCHWIDGET_DEFAULTTEXT");
                    searchViewModel.ImageUrl = Utilities.GetImageUrl(portal.ClientId, portal.PortalId, "SearchButton.png");
                }
                else
                {
                    searchViewModel.DisplaySearch = false;
                }
                return searchViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private HotTopicsViewModel CreateHotTopicsViewModel(HotTopicsWidget hotTopicsWidget)
        {
            try
            {
                HotTopicsViewModel model = new HotTopicsViewModel();
                model.clientId = portal.ClientId;
                model.portalId = portal.PortalId;
                if (hotTopicsWidget != null)
                {
                    model.Title = Utilities.GetResourceText(Resources, "HOTTOPICS_TITLE");
                    model.Days = hotTopicsWidget.MaxDays;
                    model.ArticlesCount = hotTopicsWidget.MaxItemCount;
                    model.ContentList = this._articleManager.GetHotTopics(
                        n: model.ArticlesCount,
                        dayCount: model.Days);
                }
                return model;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private FavoritesViewModel CreateFavoritesViewModel(FavoritesWidget favoritesWidget)
        {
            try
            {
                FavoritesViewModel model = new FavoritesViewModel();
                model.clientId = portal.ClientId;
                model.portalId = portal.PortalId;
                if (favoritesWidget != null)
                {
                    model.Resources = Resources;
                    model.Title = Utilities.GetResourceText(Resources, "FAVORITES_TITLE");
                    model.Days = favoritesWidget.MaxDays;
                    model.ArticlesCount = favoritesWidget.MaxItemCount;
                    model.MyFavorites = (portal.PortalType == PortalType.Personal || portal.PortalType == PortalType.Secure) ? true : false;
                    model.ContentList = this._articleManager.GetFavorites(
                        n: model.ArticlesCount,
                        dayCount: model.Days);
                }
                return model;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }
        private AttributesViewModel CreateAttributesViewModel(AttributesWidget attributesWidget)
        {
            try
            {
                AttributesViewModel attributesViewModel = new AttributesViewModel();
                attributesViewModel.clientId = portal.ClientId;
                attributesViewModel.portalId = portal.PortalId;
                if (attributesWidget != null)
                {
                    attributesViewModel.Title = Utilities.GetResourceText(Resources, "ATTRIBUTESWIDGET_TITLE");
                    attributesViewModel.AttributesList = new List<AttributeViewModel>();
                    foreach (ArticleAttribute item in attributesWidget.Attributes)
                    {
                        AttributeViewModel attributeViewModel = new AttributeViewModel()
                        {
                            ArticleAttribute = item
                        };
                        attributesViewModel.AttributesList.Add(attributeViewModel);
                    }
                }
                return attributesViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }
        private CategoriesViewModel CreateCategoriesViewModel(CategoriesWidget categoriesWidget)
        {
            try
            {
                CategoriesViewModel categoriesViewModel = new CategoriesViewModel();
                categoriesViewModel.clientId = portal.ClientId;
                categoriesViewModel.portalId = portal.PortalId;
                if (categoriesWidget != null)
                {
                    categoriesViewModel.Title = Utilities.GetResourceText(Resources, "CATEGORIESWIDGET_TITLE");
                    categoriesViewModel.Categories = ProcessCategories(categoriesWidget.Categories);
                }
                categoriesViewModel.BrowseEnabled = false;
                CategoryBrowseModule catModule = (CategoryBrowseModule)portal.Configuration.BrowseModule;
                if (catModule != null)
                {
                    categoriesViewModel.BrowseEnabled = true;
                }
                return categoriesViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private List<CategoryViewModel> ProcessCategories(List<Category> categories)
        {
            try
            {
                List<CategoryViewModel> list = new List<CategoryViewModel>();

                foreach (Category item in categories)
                {
                    CategoryViewModel categoryViewModel = new CategoryViewModel()
                    {
                        Category = item
                    };
                    categoryViewModel.ContentItems = new List<CategoryBrowse>();
                    categoryViewModel.ContentItems = _categoryManager.GetCategoriesByCategory(item.Id);
                    list.Add(categoryViewModel);
                }
                return list;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private List<ArticlesFromCategoryViewModel> CreateArticlesFromCategoryViewModelList(List<ArticlesFromCategoryWidget> articlesFromCategoryWidgets)
        {
            try
            {
                bool isBrowseEnabled = false;
                CategoryBrowseModule catModule = (CategoryBrowseModule)portal.Configuration.BrowseModule;
                if (catModule != null)
                {
                    isBrowseEnabled = true;
                }

                List<ArticlesFromCategoryViewModel> list = new List<ArticlesFromCategoryViewModel>();

                if (articlesFromCategoryWidgets != null)
                {
                    foreach (ArticlesFromCategoryWidget item in articlesFromCategoryWidgets)
                    {
                        ArticlesFromCategoryViewModel model = new ArticlesFromCategoryViewModel();
                        model.clientId = portal.ClientId;
                        model.portalId = portal.PortalId;
                        model.Title = Utilities.GetResourceText(Resources, "ARTICLESFROMCATEGORY_" + item.Id + "_TITLE");
                        model.Days = item.MaxDays;
                        model.ArticlesCount = item.MaxItemCount;
                        model.CategoryId = item.CategoryId;
                        model.ContentList = _articleManager.GetArticlesFromCategory(item.CategoryId, item.MaxItemCount, item.MaxDays);
                        model.BrowseEnabled = isBrowseEnabled;
                        model.CategoryName = GetCategoryNameById(item.CategoryId);
                        model.MoreText = Utilities.GetResourceText(Resources, "ARTICLESFROMCATEGORY_" + item.Id + "_MORE");
                        list.Add(model);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetViewModel, KBErrorHandler.GetMethodName(), GeneralResources.HomePageError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }
        private string GetCategoryNameById(int catid)
        {
            string sCatName = string.Empty;
            try
            {
                if (catid > 0)
                {
                    CategoryBrowseModule CatModule = (CategoryBrowseModule)portal.Configuration.BrowseModule;
                    if (CatModule != null)
                    {
                        List<ListItem> catItems = CatModule.Children;

                        ListItem cat = catItems.FirstOrDefault(Q => Q.Id == catid);
                        if (cat != null)
                        {
                            sCatName = cat.Name;
                        }
                    }
                }
                return sCatName;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetCategories, KBErrorHandler.GetMethodName(), GeneralResources.Art_Categories, new KBExceptionData("catid", catid));
                throw kbCustExp;
            }
        }

        private List<CustomMessageViewModel> CreateCustomMessageViewModelList(List<CustomMessageWidget> customMessageWidgets)
        {
            try
            {
                List<CustomMessageViewModel> list = new List<CustomMessageViewModel>();

                if (customMessageWidgets != null)
                {
                    foreach (CustomMessageWidget item in customMessageWidgets)
                    {
                        CustomMessageViewModel model = new CustomMessageViewModel();
                        model.clientId = portal.ClientId;
                        model.portalId = portal.PortalId;
                        model.Title = Utilities.GetResourceText(Resources, "CUSTOMMESSAGE_" + item.Id + "_TITLE");
                        model.Message = item.Message;
                        list.Add(model);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetViewModel, KBErrorHandler.GetMethodName(), GeneralResources.HomePageError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }

        private void ReadDataFromRouteData()
        {
            try
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
                this.portal = Session.GetPortalSessions().GetPortalSession(portalId, clientId).Portal;
                //Assign portal and user object to artilceManger
                this._articleManager.Portal = portal;
                this._articleManager.User = HttpContext.Session.GetUser(portalId);
                this._searchManager.Portal = portal;
                this._searchManager.User = HttpContext.Session.GetUser(portalId);
                this._categoryManager.Portal = portal;
                this._categoryManager.User = HttpContext.Session.GetUser(portalId);

                //resource object
                Resources = Session.Resource(portalId, clientId, "home", portal.Language.Name);
                CommonResources = Session.Resource(portalId, clientId, "common", portal.Language.Name);
                ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId, portal.PortalType, this.portal.Configuration, "home");
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.HomePage, KBErrorHandler.GetMethodName(), GeneralResources.HomePageError,
                    new KBExceptionData("portal.ClientId", portal.ClientId), new KBExceptionData("portal.PortalId", portal.PortalId));
                throw kbCustExp;
            }
        }
    }
}