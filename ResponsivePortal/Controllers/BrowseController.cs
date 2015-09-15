using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NLog;
using PortalAPI.Managers.Concrete;
using PortalAPI.Models;
using ResponsivePortal.Models;
using ResponsivePortal.Filters.MVC;
using ResponsivePortal.Filters;
using System.Web.Routing;
using KBCommon.KBException;
using ResponsivePortal.Resources;

namespace ResponsivePortal.Controllers
{
    [PortalConfigurationAction]
    [CustomErrorHandler]
    public class BrowseController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private ArticleManager _articleManager;
        private CategoryManager _categoryManager;
        private SearchesManager _searchesManager;
        private int clientId;
        private int portalId;
        private Portal _portal;
        public Dictionary<string, string> resources = new Dictionary<string, string>();
        public Dictionary<string, string> CommonResources = new Dictionary<string, string>();
        public HeaderViewModel headerVM;
        private string homeText = string.Empty;
        private string browseText = string.Empty;
        private CategoryBrowseModule catModule;

        public BrowseController(ArticleManager articleManager, CategoryManager categoryManager, SearchesManager searchesManager)
        {
            this._articleManager = articleManager;
            this._categoryManager = categoryManager;
            this._searchesManager = searchesManager;
        }
        //
        // GET: /Articles/
        public ActionResult Index(string title)
        {
            try
            {
                ReadDataFromRouteData();
                CategoryBrowseMainViewModel categoryBrowseMainViewModel = new CategoryBrowseMainViewModel();
                List<CategoryBrowseViewModel> categoryBrowseViewModel = new List<CategoryBrowseViewModel>();
                categoryArticleViewModel categoryArticleViewModel = new categoryArticleViewModel();
                categoryBrowseMainViewModel.CategoryBrowseViewModel = categoryBrowseViewModel;
                categoryBrowseMainViewModel.CategoryBrowseArticleViewModel = categoryArticleViewModel;
                List<CategoryBrowseViewModel> appliedCategories = new List<CategoryBrowseViewModel>();
                categoryBrowseMainViewModel.AppliedCategories = appliedCategories;
                CategoryBrowseHeaderViewModel categoryBrowseHeader = new CategoryBrowseHeaderViewModel();
                categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel = categoryBrowseHeader;
                categoryBrowseMainViewModel.portalId = portalId;
                categoryBrowseMainViewModel.clientId = clientId;
                categoryBrowseMainViewModel.Resources = resources;
                categoryBrowseMainViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
               
                if (catModule != null)
                {
                    List<ListItem> catItemList = catModule.Children;

                    TempData["MainLayoutViewModel"] = Session["MainLayoutViewModel"];

                    categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel.Categories = new List<CategoryViewModel>();
                    // TODO : change MOCK for real

                    BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                    BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = browseText, Value = "browse", Selected = true });
                    categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel.BreadcrumbViewModel = BreadcrumbViewModel;
                    foreach (var categoryItem in catItemList)
                    {
                        Category category = new Category()
                        {
                            Name = categoryItem.Name,
                            Id = categoryItem.Id,
                            ImageUrl = categoryItem.ImageUrl
                        };

                        CategoryViewModel categoryViewModel = new CategoryViewModel();
                       
                        List<CategoryBrowse> CatList = _categoryManager.GetCategoriesByCategory(Convert.ToInt32(categoryItem.Id));
                        // _categoryManager.GetCategoriesByCategory(item.Id, strGroupids);

                        //Moke Data to be removed.
                        List<Category> children = new List<Category>();
                        categoryViewModel.Category = category;
                        categoryViewModel.Category.Children = children;
                        foreach (CategoryBrowse item in CatList)
                        {
                            categoryViewModel.Category.Children.Add(new Category
                            {
                                Id = item.Id,
                                Name = item.Title
                            });
                        }
                        
                        categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel.Categories.Add(categoryViewModel);
                    }

                    categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
                    //todo
                    categoryBrowseMainViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
                    categoryBrowseMainViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
                    return View(categoryBrowseMainViewModel);
                }
                return null;

            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        [ValidateInput(false)]
        public ActionResult categoryBrowse(int? catId, string title, string searchText, string pTitle, int page = 1, bool paging = false, bool updateFilter = false, int pcatId = 0, bool search = false)
        {
            CategoryBrowseMainViewModel categoryBrowseMainViewModel = new CategoryBrowseMainViewModel();
            ReadDataFromRouteData();
            categoryBrowseMainViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
            categoryBrowseMainViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
            SearchModule SModule = (SearchModule)_portal.Configuration.SearchModule;       
            if (!paging)
            {
                if (!updateFilter)
                {
                    if (catId.HasValue && pcatId != 0 && title != null && pTitle !=null)
                    {
                        AddcategoryFiltersToSession (pcatId,pTitle,portalId,true, true);
                        AddcategoryFiltersToSession (catId.Value,title,portalId,false);
                    }
                    else {
                    AddcategoryFiltersToSession(catId.HasValue?catId.Value:pcatId, title, portalId, (pcatId != 0 && catId == null));
                    }
                }
                else
                {
                    ApplyCategoryFilter(catId.HasValue ? catId.Value : 0, title, clientId, portalId);
                }
                if (pcatId != 0 &&  (catId == null  || catId.Value ==0))
                {
                    categoryBrowseMainViewModel.ParentcategorySelected = pcatId;
                    catId = pcatId;
                }
                else
                {
                    categoryBrowseMainViewModel.ParentcategorySelected = pcatId;                   
                }
                               
             categoryBrowseMainViewModel.portalId = portalId;
             categoryBrowseMainViewModel.clientId = clientId;
             categoryBrowseMainViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
             categoryBrowseMainViewModel.Resources = resources;
             categoryBrowseMainViewModel.Page = page;
             categoryBrowseMainViewModel.ResultsPerPage =  SModule.ResultsPerPage;
             categoryBrowseMainViewModel.ResultsDisplay = SModule.ResultsDisplay;
             AddcategoryBrowseResultToSession(categoryBrowseMainViewModel, portalId);
             categoryBrowseMainViewModel.SearchImageUrl = Utilities.GetImageUrl(_portal.ClientId, _portal.PortalId, "SearchButton.png");
             categoryBrowseMainViewModel.AppliedCategories = Session.GetListOfcategoryApplied().CategoriesApplied[portalId];
             categoryBrowseMainViewModel.LastcategoryIdSelected = Session.GetListOfcategoryApplied().CategoriesApplied[portalId].LastOrDefault().Id;
            List<CategoryBrowseViewModel> categoryBrowseViewModel = new List<CategoryBrowseViewModel>();
            categoryArticleViewModel categoryArticleViewModel = new categoryArticleViewModel();
            categoryBrowseMainViewModel.CategoryBrowseViewModel = categoryBrowseViewModel;
            categoryBrowseMainViewModel.CategoryBrowseArticleViewModel = categoryArticleViewModel;
            List<CategoryBrowseViewModel> appliedCategories = new List<CategoryBrowseViewModel>();

            CategoryBrowseHeaderViewModel categoryBrowseHeader = new CategoryBrowseHeaderViewModel();
            categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel = categoryBrowseHeader;
                
            BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
            BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = browseText, Value = "browse", Selected = false });
            var parentCat = Session.GetListOfcategoryApplied().CategoriesApplied[portalId].FirstOrDefault();
            if (!string.IsNullOrEmpty(searchText))
            {
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = parentCat.Title, Value = "category", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = Utilities.GetResourceText(resources, "SEARCHRESULTFOR") + searchText, Value = "searchFor", Selected = true });
            }
                else
            {
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = parentCat.Title, Value = "category", Selected = true });
            }
            categoryBrowseMainViewModel.CategoryBrowseHeaderViewModel.BreadcrumbViewModel = BreadcrumbViewModel;
           
            try
            {

                //read and update clientid & portalid from routedata

                

                //for (int i = 0; i < 5; i++)
                //{
                //    categoryBrowseMainViewModel.AppliedCategories.Add(new CategoryBrowseViewModel
                //    {
                //        Id = i,
                //        Title = "Category" + i,
                //        ChildrenCount = i
                //    });
                //}    

                categoryBrowseMainViewModel.SearchText = "";
                if (searchText != null && searchText.Length > 0)
                {
                    categoryBrowseMainViewModel.CategoryBrowseArticleViewModel.CatArticleItem = new List<CatArticleItem>();
                    _searchesManager.SolutionFinders = "";


                    if (SModule != null)
                    { //Set numberic Search enabled
                        _searchesManager.EnhancedNumericSearchEnabled = SModule.EnhancedNumericSearchEnabled;
                        //set search type
                        _searchesManager.searchType = SModule.SearchType;
                        //Maximum results
                        _searchesManager.MaxResults = SModule.MaxResults;
                        //Spell check enabled
                        _searchesManager.SpellCheckEnabled = SModule.SpellCheckEnabled;
                        //suggested Search enabled
                        _searchesManager.SuggestedSearchesEnabled = SModule.SuggestedSearchesEnabled;
                        //Synonyms Enabled
                        _searchesManager.SynonymsEnabled = SModule.SynonymsEnabled;
                        //SolutionFinder Ids for search
                        _searchesManager.SolutionFinders = string.IsNullOrEmpty(SModule.SolutionFinders) ? "" : SModule.SolutionFinders;
                        //Auto Summarization
                        _searchesManager.AutoSummarization = SModule.AutoSummarization;
                        //Sort
                        // Sort Key from Search Sort
                        List<KeyValuePair<int, string>> SearchSorted = SModule.DefaultSort.ToList();
                        SearchSorted.Sort((FirstValue, SecondValue) =>
                        {
                            return FirstValue.Key.CompareTo(SecondValue.Key);
                        }
                        );
                        _searchesManager.DefaultSort = SearchSorted.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        //
                    }
                    else
                    {
                        _searchesManager.EnhancedNumericSearchEnabled = false;
                        //set search type
                        _searchesManager.searchType = SearchType.AllWords;
                        //Maximum results
                        _searchesManager.MaxResults = 200;
                        //Spell check enabled
                        _searchesManager.SpellCheckEnabled = false;
                        //suggested Search enabled
                        _searchesManager.SuggestedSearchesEnabled = false;
                        //Synonyms Enabled
                        _searchesManager.SynonymsEnabled = false;
                        //SolutionFinder Ids for search
                        _searchesManager.SolutionFinders = "";
                    }
                    _searchesManager.SpellCheckEnabled = false;
                    SearchResult result = new SearchResult();
                    result = _searchesManager.Search(searchText, null, catId.ToString(), null);
                    _searchesManager.SetLastSearchInfo(searchText, 1, null, HttpContext.Session.SessionID, null, 0, false, false, "Browse", "fulltext", result.SearchResults.Count);

                    if(search)
                    {
                        categoryBrowseMainViewModel.SearchText = searchText;
                    }

                    Dictionary<int, int> categoryCounts;
                    categoryCounts = new Dictionary<int, int>();
                    //get content TYpe
                    string kbName = string.Empty;
                    string FileExtn = string.Empty;
                    string summary = string.Empty;
                    foreach (var results in result.SearchResults)
                    {
                        summary = string.Empty;
                         FileExtn = Utilities.GetFileType(results.FileExtn); // file extension
                        //get knowledgebase
                         KnowledgeBase KB = _portal.Knowledgebases.FirstOrDefault(Q => Q.Id == results.KbId);
                         if (KB != null)
                         {
                            kbName = KB.Name;
                         }

                        if(SModule.AutoSummarizationEnabled)
                        {
                            summary = results.Summary;
                        }


                        CatArticleItem CatArticleItem = new CatArticleItem()
                        {
                            Id = results.Id, //Artilce Id
                            Title = results.Title,
                            ModifiedDate = results.ModifiedDate,
                            KBName = kbName,
                            Attributes = _searchesManager.GetAttributes(results.ArtAttributeIds.ToString()),
                            Extension = FileExtn,
                            Summary = summary
                        };
                        categoryBrowseMainViewModel.CategoryBrowseArticleViewModel.CatArticleItem.Add(CatArticleItem);

                        // Get article categories
                        string[] articleCats = null;
                        if (!string.IsNullOrEmpty(results.ArtCategoryIds))
                        {
                            articleCats = results.ArtCategoryIds.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                            if (articleCats.Length == 0)
                            {
                                if (categoryCounts.ContainsKey(0)) categoryCounts[0]++;
                                else categoryCounts[0] = 1;
                            }
                            for (int j = 0; j < articleCats.Length; j++)
                            {
                                if (categoryCounts.ContainsKey(Convert.ToInt32(articleCats[j]))) categoryCounts[Convert.ToInt32(articleCats[j])]++;
                                else categoryCounts[Convert.ToInt32(articleCats[j])] = 1;
                            }
                        }
                    }
                    string scategoryname = string.Join(",", categoryCounts.Select(x => x.Key).ToArray());
                    string sarticlecount = string.Join(",", categoryCounts.Select(x => x.Value).ToArray());

                    if (scategoryname.Length > 0)
                    {
                        List<SearchResultRefinement> searchresultrefinment = new List<SearchResultRefinement>();
                        searchresultrefinment = _searchesManager.GetSearchCategories(scategoryname, sarticlecount, Convert.ToInt32(catId));
                        foreach (var cats in searchresultrefinment)
                        {
                            categoryBrowseMainViewModel.CategoryBrowseViewModel.Add(new CategoryBrowseViewModel
                            {
                                Id = cats.FilterId,
                                Title = cats.Title,
                                ChildrenCount = cats.ArticleCount
                            });
                       }
                    }
                }

                else
                {
                    List<CategoryBrowse> CatList = _categoryManager.GetCategoriesByCategory(Convert.ToInt32(catId));
                    // _categoryManager.GetCategoriesByCategory(item.Id, strGroupids);

                    //Moke Data to be removed.
                    foreach (CategoryBrowse item in CatList)
                    {
                        categoryBrowseMainViewModel.CategoryBrowseViewModel.Add(new CategoryBrowseViewModel
                        {
                            Id = item.Id,
                            Title = item.Title,
                            ChildrenCount = item.ChildrenCount
                        });
                    }

                    List<CatArticleItem> CatArticlesList = _categoryManager.GetArticlesByCategory(Convert.ToInt32(catId));
                    categoryBrowseMainViewModel.CategoryBrowseArticleViewModel.CatArticleItem = CatArticlesList;                   
                }
                
                List<CategoryViewModel> categories = new List<CategoryViewModel>();
                categoryBrowseHeader.Categories = categories;
                foreach (var categoryItem in catModule.Children)
                {
                    Category category = new Category()
                    {
                        Name = categoryItem.Name,
                        Id = categoryItem.Id,
                        ImageUrl = categoryItem.ImageUrl,
                        ImageSelectedUrl = categoryItem.ImageSelectedUrl

                    };
                    CategoryViewModel categoryViewModel = new CategoryViewModel();
                    categoryViewModel.Category = category;
                    categoryBrowseHeader.Categories.Add(categoryViewModel);
                }


               
                }
                catch (Exception ex)
                {
                    logger.ErrorException("Error:" + " " + ex.Message, ex);
                    throw ex;
                }
            }
            else
            {
                try
                {
                    categoryBrowseMainViewModel = Session.GetcategoryBrowseResult().DictioinaryOfcategoryArticleViewModel[portalId];
                    categoryBrowseMainViewModel.Page = page;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
            }

            return View(categoryBrowseMainViewModel);
            
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
            _portal = Session.GetPortalSessions().GetPortalSession(portalId, clientId).Portal;

            catModule = (CategoryBrowseModule)_portal.Configuration.BrowseModule;
            if (catModule == null)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(null, KBOp.LoadBrowsePage, KBErrorHandler.GetMethodName(), GeneralResources.BrowseModuleNotFoundError, LogEnabled.False);
                throw kbCustExp;
            }
            //Assign portal and user object to artilceManger
            this._categoryManager.Portal = _portal;
            this._categoryManager.User = HttpContext.Session.GetUser(portalId);
            //Assign portal and user object to artilceManger
            this._articleManager.Portal = _portal;
            this._articleManager.User = HttpContext.Session.GetUser(portalId);

            this._searchesManager.Portal = _portal;
            this._searchesManager.User = HttpContext.Session.GetUser(portalId);

            ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId, this._portal.PortalType, this._portal.Configuration, "browse");

            //get Module name and create navigation
            headerVM = ((CommonViewModel)ViewData["CommonViewModel"]).HeaderViewModel;
            homeText = Utilities.getModuleText(headerVM, "home");
            browseText = Utilities.getModuleText(headerVM, "browse");
            //resource object
            resources = Session.Resource(portalId, clientId, "browse", _portal.Language.Name);
            CommonResources = Session.Resource(portalId, clientId, "common", _portal.Language.Name);

        }


        private void ApplyCategoryFilter(int catId, string title, int clientId, int portalId)
        {
            try
            {
                if (Session.GetListOfcategoryApplied().CategoriesApplied.ContainsKey(portalId))
                {
                    var storedCatFilters = Session.GetListOfcategoryApplied().CategoriesApplied[portalId];
                    var pos = storedCatFilters.FindIndex(item => item.Id == catId);
                    for (int i = pos + 1; i < storedCatFilters.Count; i++)
                    {
                        storedCatFilters.RemoveAt(i);
                    }
                    Session.GetListOfcategoryApplied().CategoriesApplied[portalId] = storedCatFilters;

                }

            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        private void AddcategoryFiltersToSession(int catId, string tilte, int portaId, bool parent, bool clearAll = false)
        {
            CategoryBrowseViewModel parentInSession = null;
            var dictionary = Session.GetListOfcategoryApplied();
            if (dictionary != null)
            {
                if (dictionary.CategoriesApplied.ContainsKey(portalId))
                {
                    if (clearAll) dictionary.CategoriesApplied[portalId].Clear();
                    if (parent)
                    {
                        parentInSession = dictionary.CategoriesApplied[portalId].FirstOrDefault();
                        dictionary.CategoriesApplied[portalId].Clear();
                    }
                    if (dictionary.CategoriesApplied[portalId].Find(filter => filter.Id == catId) == null)
                    {
                        if (tilte != null)
                        {
                            dictionary.CategoriesApplied[portalId].Add(new CategoryBrowseViewModel { Id = catId, Title = tilte });
                            Session["DictionaryOfcategoryApplied"] = dictionary;
                        }
                        else if (parentInSession !=null && parentInSession.Id == catId)
                        {
                            dictionary.CategoriesApplied[portalId].Add(new CategoryBrowseViewModel { Id = catId, Title = parentInSession.Title });
                            Session["DictionaryOfcategoryApplied"] = dictionary;
                        }
                    }

                }
                else
                {
                    var list = new List<CategoryBrowseViewModel>();
                    list.Add(new CategoryBrowseViewModel { Id = catId, Title = tilte });
                    dictionary.CategoriesApplied.Add(portalId, list);
                    Session["DictionaryOfcategoryApplied"] = dictionary;

                }
            }
            else
            {

                var newDictionary = new DictionaryOfcategoryApplied();
                List<CategoryBrowseViewModel> list = new List<CategoryBrowseViewModel>();
                list.Add(new CategoryBrowseViewModel { Id = catId, Title = tilte });
                Dictionary<int, List<CategoryBrowseViewModel>> CategoriesApplied = new Dictionary<int, List<CategoryBrowseViewModel>>();
                newDictionary.CategoriesApplied = CategoriesApplied;
                newDictionary.CategoriesApplied[portalId] = list;
                Session["DictionaryOfcategoryApplied"] = newDictionary;

            }
        }
        private void AddcategoryBrowseResultToSession(CategoryBrowseMainViewModel result, int portaId)
        {
            var dictionary = Session.GetcategoryBrowseResult();
            if (dictionary != null)
            {
                dictionary.DictioinaryOfcategoryArticleViewModel[portalId] = result;
                Session["DictionaryOfcategoryArticle"] = dictionary;
            }
            else
            {
                var newDictionary = new DictionaryOfcategoryArticleViewModel();
                Dictionary<int, CategoryBrowseMainViewModel> dictioinaryOfcategoryArticleViewModel = new Dictionary<int, CategoryBrowseMainViewModel>();
                newDictionary.DictioinaryOfcategoryArticleViewModel = dictioinaryOfcategoryArticleViewModel;
                newDictionary.DictioinaryOfcategoryArticleViewModel[portalId] = result;
                Session["DictionaryOfcategoryArticle"] = newDictionary;
            }
        }

    }
}