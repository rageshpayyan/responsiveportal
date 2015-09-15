using PortalAPI.Managers.Concrete;
using PortalAPI.Models;
using ResponsivePortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using ResponsivePortal.Filters.MVC;
using ResponsivePortal.Filters;
using KBCommon.KBException;
using ResponsivePortal.Resources;
namespace ResponsivePortal.Controllers
{
    [PortalConfigurationAction]
    [CustomErrorHandler]
    public class SearchController : Controller
    {
        //
        // GET: /Search/
        private SearchesManager _searchesManager;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private int clientId;
        private int portalId;
        private Portal _portal;
        private User _user;
        private SearchResultsMainViewModel searchMainViewModel;
        public HeaderViewModel headerVM;
        public Dictionary<string, string> Resources = new Dictionary<string, string>();
        public Dictionary<string, string> CommonResources = new Dictionary<string, string>();

        public SearchController(SearchesManager searchesManager)
        {
            this._searchesManager = searchesManager;
            searchMainViewModel = new SearchResultsMainViewModel();
        }
      
       
        public ActionResult Index(string title)
        {
            try
            {
                //read clientid,portalid from route data
                ReadDataFromRouteData();
                //SearchResult result = _searchesManager.Search("*");
                SearchViewModel viewModel = new SearchViewModel();
                viewModel.SearchLabel = "Enter your search text";
                viewModel.Title = "Search";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        [ValidateInput(false)]
        public ActionResult GetSearch(string text, string kbid, string catid, string attributeid, string title, string contentTypeId, 
            string format, string searchFrom, int page = 1, bool filterUpdate = false, 
            bool spellCheck = true, bool fromwidget = false,int widgetsearch=0)
        {

            text = removeExtraQueryString(text);

            if (string.IsNullOrEmpty(text)) text = "*";

            
            
            // Read clientid, portalid from route data
            ReadDataFromRouteData();

            if (widgetsearch==1)
                RemoveAllFilter(text, clientId, portalId, fromwidget);

            // Prepare Breadcrumb navigation.
            // Get Module name and create navigation
            string homeText = Utilities.getModuleText(headerVM, "home");
            
            BreadcrumbViewModel breadcrumbViewModel = new BreadcrumbViewModel();
            breadcrumbViewModel.NavigationList = new List<SelectListItem>();
            breadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });

            // Set searchMainViewModel fields
            searchMainViewModel.BreadcrumbViewModel = breadcrumbViewModel;
            searchMainViewModel.clientId = clientId;
            searchMainViewModel.portalId = portalId;
            searchMainViewModel.Resources = Resources;
            searchMainViewModel.SearchImageUrl = Utilities.GetImageUrl(_portal.ClientId, _portal.PortalId, "SearchButton.png");
            searchMainViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
            searchMainViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
            SearchModule searchModule = (SearchModule)_portal.Configuration.SearchModule;
            searchMainViewModel.SearchResultsPerPage =  searchModule.ResultsPerPage;
            searchMainViewModel.FilterDisplay = InitializeFilterDisplay(searchModule);
            searchMainViewModel.ResultsDisplay = searchModule.ResultsDisplay;  
            if (fromwidget)
            {
                searchMainViewModel.FromWidget = true; // for category and attribute widget
            }
            SetSearchManagerValues(spellCheck, searchModule);
            GetFilterValues(ref kbid, ref catid, ref attributeid, title, ref contentTypeId, ref format, searchFrom);

            try
            {
                int searchid = 0;
                SearchResult searchResult = SetLastSearchResult(text, kbid, catid, attributeid, contentTypeId, format, searchFrom, filterUpdate, out searchid);

                searchResult = SortSearchResult(_searchesManager.DefaultSort, searchResult);

                searchMainViewModel.Searchtext = text;
                searchMainViewModel.Page = page;
                searchMainViewModel.SearchResultsViewModel = new List<SearchResultsViewModel>();
                searchMainViewModel.KnowledgeBases = new List<SearchResultRefinement>();
                searchMainViewModel.ContentTypes = new List<SearchResultContentTypeRefinement>();
                searchMainViewModel.Format = new List<SearchResultFormatRefinement>();
                searchMainViewModel.SearchId = searchid;

                Dictionary<int, int> categoryCounts = new Dictionary<int, int>();
                Dictionary<int, int> attributeCounts = new Dictionary<int, int>();
                Dictionary<int, int> kBCounts = new Dictionary<int, int>();
                Dictionary<string, List<string>> contentTypeCounts = new Dictionary<string, List<string>>();
                Dictionary<string, string> fileFormatCounts = new Dictionary<string, string>();
                Dictionary<int, string> dCatName = new Dictionary<int, string>();
                Dictionary<string, string> dContentTypes = new Dictionary<string, string>();

                string contentName = string.Empty;
                string contentSource = string.Empty;
                if (searchResult != null)
                {
                    foreach (var results in searchResult.SearchResults)
                    {
                        string attributeNames = GetArticleNames(results);
                        string fileExtn = Utilities.GetFileType(results.FileExtn);
                        string kbName = GetKBName(results);
                        string autoSummEnabled = GetAutoSummarizationEnabled(searchModule, results, text);

                        AddSearchResultsViewModel(attributeNames, autoSummEnabled, results, fileExtn, kbName);
                        GetArticleCounts(categoryCounts, attributeCounts, kBCounts, results);

                        contentName = results.SourceName.ToString();
                        contentSource = results.SourceType;

                        GetContentTypeCounts(contentTypeCounts, contentName, contentSource);
                        GetFileFormatCounts(fileFormatCounts, results, fileExtn);
                    }

                    SetSearchMainViewModelValues(catid, attributeid, searchResult, categoryCounts, attributeCounts, kBCounts, contentTypeCounts, fileFormatCounts);
                }
                Session.Add("SearchText_" + portalId.ToString(), text);
                return View(searchMainViewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSearch, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError,
                    new KBExceptionData("text", text), new KBExceptionData("kbid", kbid), new KBExceptionData("catid", catid), new KBExceptionData("attributeid", attributeid),
                    new KBExceptionData("title", title), new KBExceptionData("contentTypeId", contentTypeId));
                throw kbCustExp;
            }
        }

        private static string removeExtraQueryString(string querystring)
        {
            if(querystring.Contains("="))
            {
                string[] splits = querystring.Split('&');
                if (splits.Length > 0)
                    return splits[0];
            }
            return querystring;
        }
        private string GetAutoSummarizationEnabled(SearchModule searchModule, SearchItem results, string searchString)
        {
            string autoSummEnabled = string.Empty;
            if (searchModule.AutoSummarizationEnabled)
            {
                if (_portal.ArticleTemplateBodySecurity)
                {
                    if (results.ArtTemplateId > 0)
                    {
                        string groups = string.Join(",", _portal.ArticleGroups.Select(arr => arr.Id.ToString()));
                        autoSummEnabled = _searchesManager.GetArticleSecuredSummary(results.Id, results.KbId, groups, results.FileExtn, searchModule.AutoSummarization.MaxLength, searchString, searchModule.ResultsDisplay.HighlightTerm);
                    }
                    else
                    {
                        autoSummEnabled = results.Summary;
                    }
                }
                else
                {
                    autoSummEnabled = results.Summary;
                }
            }
            return autoSummEnabled;
        }

        private string GetArticleNames(SearchItem results)
        {
            string AttributeNames = string.Empty;

            // Get article names from DB
            if (!string.IsNullOrEmpty(results.ArtAttributeIds))
            {
                if (results.ArtAttributeIds != "0")
                {
                    var Attributes = _searchesManager.GetAttributesByIds(results.ArtAttributeIds.ToString());
                    if (Attributes.Count > 0)
                    {
                        AttributeNames = String.Join(",", Attributes);
                    }
                }
            }
            return AttributeNames;
        }

        private string GetKBName(SearchItem results)
        {
            string kbName = string.Empty;
            if (results.SourceType == "Articles")
            {
                KnowledgeBase KB = _portal.Knowledgebases.FirstOrDefault(Q => Q.Id == results.KbId);
                if (KB != null)
                {
                    kbName = KB.Name;
                }
            }
            return kbName;
        }

        private  void GetFileFormatCounts(Dictionary<string, string> fileFormatCounts, SearchItem results, string FileExtn)
        {
            if (!fileFormatCounts.ContainsKey(FileExtn))
            {
                string extension = GetOfficeExtensions(FileExtn);
                if (extension != string.Empty)
                {
                    fileFormatCounts.Add(FileExtn, extension);
                }
                else
                {
                    fileFormatCounts.Add(FileExtn, results.FileExtn);
                }
            }
        }
        private string GetOfficeExtensions(string FileExtn)
        {
            string officeExtension = string.Empty;
            if (FileExtn.ToLower() == "word" || FileExtn.ToLower() == "excel" || FileExtn.ToLower() == "ppt")
            {
                switch (FileExtn.ToLower())
                {
                    case "word":
                        officeExtension = "doc,docx";
                        break;
                    case "excel":
                        officeExtension = "xls,xlsx";
                        break;
                    case "ppt":
                        officeExtension = "ppt,pptx";
                        break;
                    default:
                        officeExtension = string.Empty;
                        break;
                }
            }
            return officeExtension;
        }
        private  void GetContentTypeCounts(Dictionary<string, List<string>> contentTypeCounts, string contentName, string contentSource)
        {
            List<string> contentTypeCountsList;
            contentSource = Utilities.GetResourceText(Resources, "FILTERDISPLAY_CONTENTTYPE_" + contentSource.ToUpper() + "_TITLE");
            if (!contentTypeCounts.TryGetValue(contentSource, out contentTypeCountsList))
            {
                contentTypeCountsList = new List<string>();               
                contentTypeCounts.Add(contentSource, contentTypeCountsList);
            }
            if (!contentTypeCountsList.Contains(contentName))
            {
                contentTypeCountsList.Add(contentName);
            }
        }

        private  FilterDisplayValues InitializeFilterDisplay(SearchModule SModule)
        {
            FilterDisplayValues filterDisplay = new FilterDisplayValues()
            {
                Kb = SModule.IsDisplayFilterEnabled(FilterDisplay.kb),
                Categories = SModule.IsDisplayFilterEnabled(FilterDisplay.categories),
                Attributes = SModule.IsDisplayFilterEnabled(FilterDisplay.attributes),
                ContentTypes = SModule.IsDisplayFilterEnabled(FilterDisplay.contentTypes),
                Formats = SModule.IsDisplayFilterEnabled(FilterDisplay.formats),
            };
            return filterDisplay;
        }

        private void SetSearchMainViewModelValues(string catid, string attributeid, SearchResult searchResult, Dictionary<int, int> categoryCounts, Dictionary<int, int> attributeCounts, Dictionary<int, int> KBCounts, Dictionary<string, List<string>> contentTypeCounts, Dictionary<string, string> FileFormatCounts)
        {
            foreach (var item in KBCounts)
            {
                if (item.Key != 0)
                {
                    SearchResultRefinement knowledgebases = new SearchResultRefinement()
                    {
                        FilterId = _portal.Knowledgebases.Find(r => r.Id == item.Key).Id,
                        Title = _portal.Knowledgebases.Find(r => r.Id == item.Key).Name,
                        ArticleCount = item.Value
                    };
                    searchMainViewModel.KnowledgeBases.Add(knowledgebases);
                }
            }

            string scategoryname = string.Join(",", categoryCounts.Select(x => x.Key).ToArray());
            string sarticlecount = string.Join(",", categoryCounts.Select(x => x.Value).ToArray());

            if (scategoryname.Length > 0)
            {
                searchMainViewModel.Categories = _searchesManager.GetSearchCategories(scategoryname, sarticlecount, Convert.ToInt32(catid));
            }

            string sattributename = string.Join(",", attributeCounts.Select(x => x.Key).ToArray());
            string sattributecount = string.Join(",", attributeCounts.Select(x => x.Value).ToArray());

            if (sattributename.Length > 0)
            {
                searchMainViewModel.Attributes = _searchesManager.GetSearchAttributes(sattributename, sattributecount, Convert.ToInt32(attributeid));
            }

            foreach (var item in contentTypeCounts)
            {
                SearchResultContentTypeRefinement contenttypes = new SearchResultContentTypeRefinement()
                {
                    ContentName = item.Key,
                    ContentIds = string.Join(",", item.Value.Aggregate((x, y) => x + "," + y))

                };
                searchMainViewModel.ContentTypes.Add(contenttypes);
            }

            foreach (var item in FileFormatCounts)
            {
                SearchResultFormatRefinement fileformat = new SearchResultFormatRefinement()
                {
                    Format = item.Key,
                    Extension = item.Value
                };
                searchMainViewModel.Format.Add(fileformat);
            }

            searchMainViewModel.SuggestionSearch = searchResult.SuggestedTerms;
            searchMainViewModel.SpellingCorrection = searchResult.SpellCheckWords;
            searchMainViewModel.ModifiedSearchText = searchResult.ModifiedSearchText;
            searchMainViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
        }

        /// <summary>
        /// Set Last Search Result in Session or in SessionManager.
        /// </summary>
        private SearchResult SetLastSearchResult(string text, string kbid, string catid, string attributeid, string contentTypeId, string format, string searchFrom, bool filterUpdate, out int searchid)
        {
            SearchResult searchResult = new SearchResult();
            int uid = _user == null ? 0 : _user.UserId;
            searchid = 0;
            //if search from home page clear all filters in session, do search and store result and search text.
            if (!string.IsNullOrEmpty(searchFrom) && searchFrom == SearchFrom.HomePage.ToString())
            {
                Session.RemovelAllSearchFilters(portalId);
                searchResult = _searchesManager.Search(text);
                // Sort search result
                //_searchesManager.DefaultSort.

                //
                Session.StoreLastSearchResult(searchResult, portalId);
                searchid = _searchesManager.SetLastSearchInfo(text, 1, null, HttpContext.Session.SessionID, null, uid, false, false, "Home", "fulltext", searchResult.SearchResults.Count);                
            }
            // if navigatiing from article page or paging no autonomy call just read what is stored in session.    
            else if (SearchFrom.Paging.ToString() == searchFrom || SearchFrom.ArticlePage.ToString() == searchFrom
                || SearchFrom.SolutionFinder.ToString() == searchFrom)
            {
                searchResult = Session.GetLastSearchResult(portalId);
            }
            // if filters have have removed, make autonomy call and store result.
            else if (searchFrom == SearchFrom.SearchPage.ToString() || filterUpdate
                || searchFrom == SearchFrom.HomeWidget.ToString() || searchFrom == SearchFrom.SearchInsteadFor.ToString()
                || searchFrom == SearchFrom.Suggestion.ToString())
            {
                searchResult = _searchesManager.Search(text, kbid, catid, attributeid);

                Session.StoreLastSearchResult(searchResult, portalId);
                if (!filterUpdate)
                {
                    searchid= _searchesManager.SetLastSearchInfo(text, 1, null, HttpContext.Session.SessionID, null, uid, false, false, "Search", "fulltext", searchResult.SearchResults.Count);                    
                }
            }
            //if filter by contentType/ format no autonomy call, filter in session and store result.
            else if (!string.IsNullOrEmpty(contentTypeId) || !string.IsNullOrEmpty(format))
            {
                if (!string.IsNullOrEmpty(contentTypeId))
                {
                    searchResult = Session.GetLastSearchResult(portalId);
                    var ids = contentTypeId.ToLower().Split(',').ToList();
                    var filteredResult = (from item in searchResult.SearchResults where ids.Contains(item.SourceName.ToLower()) select item).ToList<SearchItem>();
                    searchResult.SearchResults = (List<SearchItem>)filteredResult;

                    Session.StoreLastSearchResult(searchResult, portalId);
                }
                else if (!string.IsNullOrEmpty(format))
                {
                    searchResult = Session.GetLastSearchResult(portalId);
                    var filteredResult = (from item in searchResult.SearchResults where format.ToLower().Split(',').ToList().Contains(item.FileExtn.ToLower()) select item).ToList<SearchItem>();
                    searchResult.SearchResults = filteredResult;
                    Session.StoreLastSearchResult(searchResult, portalId);
                }

            }
            else  // make autonomy call with avalable filters.
            {
                searchResult = _searchesManager.Search(text, kbid, catid, attributeid);
                Session.StoreLastSearchResult(searchResult, portalId);
            }

            if (searchResult!=null )
                searchResult.SearchText = text;          

            return searchResult;
        }

        private void AddSearchResultsViewModel(string AttributeNames, string summary, SearchItem results, string FileExtn, string kbName)
        {
            SearchResultsViewModel searchResultsModel = new SearchResultsViewModel()
            {
                Id = results.Id, //Artilce Id
                SFId = results.SFId,  // Solution FInder Id
                SFCId = results.SFCId, // Solution Finder Choice Id
                Title = results.Title,
                Summary = summary,
                Modified = results.ModifiedDate.ToShortDateString(),
                KBName = kbName,
                SourceName = results.SourceName,
                SourceType = results.SourceType,
                Size = results.FileSize,
                Attributes = AttributeNames,
                FileType = FileExtn
            };

            searchMainViewModel.SearchResultsViewModel.Add(searchResultsModel);
        }

        private static void GetArticleCounts(Dictionary<int, int> categoryCounts, Dictionary<int, int> attributeCounts, Dictionary<int, int> KBCounts, SearchItem results)
        {
            // Get article KB counts
            string[] articleKBs = results.KbId.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (articleKBs.Length == 0)
            {
                if (KBCounts.ContainsKey(0)) KBCounts[0]++;
                else KBCounts[0] = 1;
            }
            for (int j = 0; j < articleKBs.Length; j++)
            {
                if (KBCounts.ContainsKey(Convert.ToInt32(articleKBs[j]))) KBCounts[Convert.ToInt32(articleKBs[j])]++;
                else KBCounts[Convert.ToInt32(articleKBs[j])] = 1;
            }

            // Get article category counts
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
            // Get article attribute counts
            if (!string.IsNullOrEmpty(results.ArtAttributeIds))
            {
                string[] articleAttributes = results.ArtAttributeIds.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (articleAttributes.Length == 0)
                {
                    if (attributeCounts.ContainsKey(0)) attributeCounts[0]++;
                    else attributeCounts[0] = 1;
                }
                for (int j = 0; j < articleAttributes.Length; j++)
                {
                    if (attributeCounts.ContainsKey(Convert.ToInt32(articleAttributes[j]))) attributeCounts[Convert.ToInt32(articleAttributes[j])]++;
                    else attributeCounts[Convert.ToInt32(articleAttributes[j])] = 1;
                }
            }
        }

        private void GetFilterValues(ref string kbid, ref string catid, ref string attributeid, string title, ref string contentTypeId, ref string format, string searchFrom)
        {
            try
            {
                //if not from home page store any new filter in session read all filtres from session.
                if (string.IsNullOrEmpty(searchFrom) || SearchFrom.HomePage.ToString() != searchFrom)
                {
                    if (catid != null)
                    {
                        ManageFiltersSession(FilterType.category, catid, title, portalId);
                    }
                    if (attributeid != null)
                    {
                        ManageFiltersSession(FilterType.Attribute, attributeid, title, portalId);
                    }
                    if (kbid != null)
                    {
                        ManageFiltersSession(FilterType.KnowledgeBase, kbid, title, portalId);
                    }
                    if (contentTypeId != null)
                    {
                        ManageFiltersSession(FilterType.ContentType, contentTypeId, title, portalId);
                    }
                    if (format != null)
                    {
                        ManageFiltersSession(FilterType.Format, format, title, portalId);
                    }

                    //read filters in session
                    LatestFilter AppliedFilters = GetLatestFilterId();
                    if (AppliedFilters != null)
                    {
                        if (AppliedFilters.KbId != null) { kbid = AppliedFilters.KbId.ToString(); }
                        if (AppliedFilters.categoryId != null) { catid = AppliedFilters.categoryId.ToString(); }
                        if (AppliedFilters.AttributeId != null) { attributeid = AppliedFilters.AttributeId.ToString(); }
                        if (AppliedFilters.ContentTypeId != null) { contentTypeId = AppliedFilters.ContentTypeId.ToString(); }
                        if (AppliedFilters.Format != null) { format = AppliedFilters.Format.ToString(); }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        private void SetSearchManagerValues(bool spellCheck, SearchModule SModule)
        {
            _searchesManager.Portal = _portal;
            _searchesManager.User = _user;
            if (SModule != null)
            {
                _searchesManager.EnhancedNumericSearchEnabled = SModule.EnhancedNumericSearchEnabled;
                _searchesManager.searchType = SModule.SearchType;
                _searchesManager.MaxResults = SModule.MaxResults;
                _searchesManager.SpellCheckEnabled = SModule.SpellCheckEnabled;
                _searchesManager.SuggestedSearchesEnabled = SModule.SuggestedSearchesEnabled;
                _searchesManager.SynonymsEnabled = SModule.SynonymsEnabled;
                _searchesManager.SolutionFinders = string.IsNullOrEmpty(SModule.SolutionFinders) ? "" : SModule.SolutionFinders;
                _searchesManager.AutoSummarization = SModule.AutoSummarization;

                // Sort Key from Search Sort
                List<KeyValuePair<int, string>> SearchSorted = SModule.DefaultSort.ToList();
                SearchSorted.Sort((FirstValue, SecondValue) =>
                {
                    return FirstValue.Key.CompareTo(SecondValue.Key);
                }
                );
                _searchesManager.DefaultSort = SearchSorted.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                //
                _searchesManager.HighlightEnabled = SModule.ResultsDisplay.HighlightTerm;
            }
            else
            {
                _searchesManager.EnhancedNumericSearchEnabled = false;
                _searchesManager.searchType = SearchType.AllWords;
                _searchesManager.MaxResults = 200;
                _searchesManager.SpellCheckEnabled = false;
                _searchesManager.SuggestedSearchesEnabled = false;
                _searchesManager.SynonymsEnabled = false;
                _searchesManager.SolutionFinders = "";
                _searchesManager.HighlightEnabled = false;
            }
            if (!spellCheck) { _searchesManager.SpellCheckEnabled = false; }
        }

        void ManageFiltersSession(FilterType filterType, string id, string title, int portalId)
        {
            try
            {
                var allPortalFilters = Session.GetAllPortalFilters();
                var filter = new SearchFiltersApplied()
                {
                    Id = id,
                    Title = title,
                    FilterType = filterType
                };

                if (allPortalFilters == null)
                {
                    var filtersDict = new DictionaryOfSearchFiltersAppliedColletion();
                    var filters = new Dictionary<FilterType, List<SearchFiltersApplied>>();
                    var filtersList = new List<SearchFiltersApplied>();
                    filtersList.Add(filter);
                    filters.Add(filterType, filtersList);
                    filtersDict.FilterCollection = filters;
                    allPortalFilters = new PortalDictionaryOfSearchFiltersApplied();
                    var filtersapplied = new Dictionary<int, DictionaryOfSearchFiltersAppliedColletion>();
                    allPortalFilters.FiltersApplied = filtersapplied;
                    allPortalFilters.FiltersApplied.Add(portalId, filtersDict);
                    Session["SearchFilersApplied"] = allPortalFilters;
                    return;
                }

                else
                {
                    var portalFilter = allPortalFilters.FiltersApplied.ContainsKey(portalId) ?
                        allPortalFilters.FiltersApplied[portalId] : null;
                    if (portalFilter != null)
                    {
                        var list = portalFilter.FilterCollection.ContainsKey(filterType) ?
                            allPortalFilters.FiltersApplied[portalId].FilterCollection[filterType] : null;
                        if (list != null)
                        {
                            if (!list.Exists(f => f.Id == filter.Id))
                            {
                                list.Add(filter);
                            }
                            allPortalFilters.FiltersApplied[portalId].FilterCollection[filterType] = list;
                            Session["SearchFilersApplied"] = allPortalFilters;
                        }
                        else
                        {
                            var filtersList = new List<SearchFiltersApplied>();
                            filtersList.Add(filter);
                            allPortalFilters.FiltersApplied[portalId].FilterCollection.Add(filterType, filtersList);
                            Session["SearchFilersApplied"] = allPortalFilters;
                        }
                    }
                    else
                    {
                        var filtersDict = new DictionaryOfSearchFiltersAppliedColletion();
                        var filters = new Dictionary<FilterType, List<SearchFiltersApplied>>();
                        var filtersList = new List<SearchFiltersApplied>();
                        filtersList.Add(filter);
                        filters.Add(filterType, filtersList);
                        filtersDict.FilterCollection = filters;
                        allPortalFilters.FiltersApplied[portalId] = filtersDict;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        [ValidateInput(false)]
        public ActionResult RemoveFilter(string text, string id, FilterType filterType, int clientId, int portalId, bool fromwidget = false)
        {
            try
            {
                var filters = Session.GetAllPortalFilters().FiltersApplied[portalId].FilterCollection[filterType];
                var pos = filters.FindIndex(item => item.Id == id);
                filters.RemoveRange(pos, filters.Count - pos);
                Session.GetAllPortalFilters().FiltersApplied[portalId].FilterCollection[filterType] = filters;
                var routeDic = GetRouteDictionary(text, clientId, portalId, fromwidget);
                return RedirectToAction("GetSearch", routeDic);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        [ValidateInput(false)]
        public ActionResult RemoveAllFilter(string text, int clientId, int portalId, bool fromwidget = false)
        {
            try
            {
                Session.RemovelAllSearchFilters(portalId);
                var routeDic = GetRouteDictionary(text, clientId, portalId, fromwidget);
                return RedirectToAction("GetSearch", routeDic);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        [ValidateInput(false)]
        public ActionResult RemoveAllFromFilterType(string text, FilterType filterType, int clientId, int portalId, bool fromwidget = false)
        {
            try
            {
                Session.GetAllPortalFilters().FiltersApplied[portalId].FilterCollection[filterType].Clear();
                var routeDic = GetRouteDictionary(text, clientId, portalId, fromwidget);
                return RedirectToAction("GetSearch", routeDic);

            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }

        [ValidateInput(false)]
        public ActionResult Paging(int page, string text, SearchFrom searchFrom, int clientId, int portalId, bool fromwidget = false)
        {
            try
            {
                var routrDict = new RouteValueDictionary();
                routrDict.Add("Controller", "Search");
                routrDict.Add("Action", "GetSearch");
                routrDict.Add("page", page);
                routrDict.Add("searchFrom", searchFrom);
                routrDict.Add("text", text);
                routrDict.Add("clientId", clientId);
                routrDict.Add("portalId", portalId);
                routrDict.Add("fromwidget", fromwidget);
                return RedirectToAction("GetSearch", routrDict);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        private RouteValueDictionary GetRouteDictionary(string text, int clientId, int portalId, Boolean fromwidget)
        {
            try
            {
                var routrDict = new RouteValueDictionary();
                routrDict.Add("Controller", "Search");
                routrDict.Add("Action", "GetSearch");
                routrDict.Add("filterUpdate", true);
                routrDict.Add("text", text);
                routrDict.Add("clientId", clientId);
                routrDict.Add("portalId", portalId);
                routrDict.Add("fromwidget", fromwidget);
                return routrDict;

            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        public ActionResult GetAutoCompletes(string pattern, int clientId, int portalId, bool fromwidget = false)
        {

            try
            {

                Cache _Cache = HttpContext.Cache;
                var clues = (Dictionary<string, int>)_Cache["CommClues_" + portalId.ToString()];
                if (clues != null)
                {
                    var machingClues = (from entry in clues
                                        where entry.Key.StartsWith(pattern)
                                        orderby entry.Value descending
                                        select entry).Take(5).ToDictionary(x => x.Key, x => x.Value);

                    return Json(machingClues.Keys, JsonRequestBehavior.AllowGet);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        private LatestFilters GetLatestFilterIds()
        {
            try
            {
                var filters = Session.GetAllPortalFilters().FiltersApplied[portalId];
                if (null != filters)
                {
                    LatestFilters latest = new LatestFilters();
                    try
                    {
                        if (filters.FilterCollection.ContainsKey(FilterType.KnowledgeBase))
                        {
                            if (filters.FilterCollection[FilterType.KnowledgeBase].Count > 0) latest.KbId = filters.FilterCollection[FilterType.KnowledgeBase].Select(l => l.Id).ToArray<string>();
                        }

                        if (filters.FilterCollection.ContainsKey(FilterType.category))
                        {
                            if (filters.FilterCollection[FilterType.category].Count > 0) latest.categoryId = filters.FilterCollection[FilterType.category].Select(l => l.Id).ToArray<string>();
                        }

                        if (filters.FilterCollection.ContainsKey(FilterType.Attribute))
                        {
                            if (filters.FilterCollection[FilterType.Attribute].Count > 0) latest.AttributeId = filters.FilterCollection[FilterType.Attribute].Select(l => l.Id).ToArray<string>();
                        }
                        if (filters.FilterCollection.ContainsKey(FilterType.ContentType))
                        {
                            if (filters.FilterCollection[FilterType.ContentType].Count > 0) latest.ContentTypeId = filters.FilterCollection[FilterType.ContentType].Select(l => l.Id).ToArray<string>();
                        }
                    }
                    catch (ArgumentException)
                    {
                    }

                    return latest;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
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
                _user = HttpContext.Session.GetUser(portalId);
                _portal = Session.GetPortalSessions().GetPortalSession(portalId,clientId).Portal;
                _searchesManager.Portal = _portal;
                _searchesManager.User = _user;
                //resource object
                Resources = Session.Resource(portalId, clientId, "search", _portal.Language.Name);
                CommonResources = Session.Resource(portalId, clientId, "common", _portal.Language.Name);
                ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId, this._portal.PortalType, this._portal.Configuration,"article");
                headerVM = ((CommonViewModel)ViewData["CommonViewModel"]).HeaderViewModel;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        private LatestFilter GetLatestFilterId()
        {
            try
            {
                var allPortalFilters = Session.GetAllPortalFilters();
                if (allPortalFilters != null)
                {
                    var filters = Session.GetAllPortalFilters().FiltersApplied[portalId] != null ?
                        Session.GetAllPortalFilters().FiltersApplied[portalId] : null;

                    if (null != filters)
                    {
                        LatestFilter latest = new LatestFilter();

                        if (filters.FilterCollection.ContainsKey(FilterType.KnowledgeBase))
                        {
                            if (filters.FilterCollection[FilterType.KnowledgeBase].Count > 0) latest.KbId = filters.FilterCollection[FilterType.KnowledgeBase].LastOrDefault().Id;
                        }
                        if (filters.FilterCollection.ContainsKey(FilterType.category))
                        {
                            if (filters.FilterCollection[FilterType.category].Count > 0) latest.categoryId = filters.FilterCollection[FilterType.category].LastOrDefault().Id;
                        }
                        if (filters.FilterCollection.ContainsKey(FilterType.Attribute))
                        {
                            if (filters.FilterCollection[FilterType.Attribute].Count > 0) latest.AttributeId = filters.FilterCollection[FilterType.Attribute].LastOrDefault().Id;
                        }
                        if (filters.FilterCollection.ContainsKey(FilterType.ContentType))
                        {
                            if (filters.FilterCollection[FilterType.ContentType].Count > 0) latest.ContentTypeId = filters.FilterCollection[FilterType.ContentType].LastOrDefault().Id;
                        }
                        if (filters.FilterCollection.ContainsKey(FilterType.Format))
                        {
                            if (filters.FilterCollection[FilterType.Format].Count > 0) latest.Format = filters.FilterCollection[FilterType.Format].LastOrDefault().Id;
                        }
                        return latest;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error:" + " " + ex.Message, ex);
                throw ex;
            }
        }
        private static SearchResult SortSearchResult(Dictionary<int, string> DefaultSort, SearchResult searchResult)
        {
            SearchResult searchResult1 = searchResult;
            Dictionary<int, string> DefaultNewSort;

            DefaultNewSort = new Dictionary<int, string>();

            if (DefaultSort != null && searchResult != null)
            {
                if (DefaultSort.Count == 3)
                {
                    if ((DefaultSort[1] == "relevance") && (DefaultSort[2] == "modifiedDate") && (DefaultSort[3] == "articleTitle"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderByDescending(a => a.Rank).ThenByDescending(b => b.ModifiedDate).ThenBy(c => c.Title).ToList();
                    }
                    if ((DefaultSort[1] == "modifiedDate") && (DefaultSort[2] == "relevance") && (DefaultSort[3] == "articleTitle"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderByDescending(a => a.ModifiedDate).ThenByDescending(b => b.Rank).ThenBy(c => c.Title).ToList();
                    }
                    if ((DefaultSort[1] == "articleTitle") && (DefaultSort[2] == "relevance") && (DefaultSort[3] == "modifiedDate"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderBy(a => a.Title).ThenByDescending(b => b.Rank).ThenByDescending(c => c.ModifiedDate).ToList();
                    }
                    if ((DefaultSort[1] == "relevance") && (DefaultSort[2] == "articleTitle") && (DefaultSort[3] == "modifiedDate"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderByDescending(a => a.Rank).ThenBy(b => b.Title).ThenByDescending(c => c.ModifiedDate).ToList();
                    }
                    if ((DefaultSort[1] == "articleTitle") && (DefaultSort[2] == "modifiedDate") && (DefaultSort[3] == "relevance"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderBy(a => a.Title).ThenByDescending(b => b.ModifiedDate).ThenByDescending(c => c.Rank).ToList();
                    }
                    if ((DefaultSort[1] == "modifiedDate") && (DefaultSort[2] == "articleTitle") && (DefaultSort[3] == "relevance"))
                    {
                        searchResult1.SearchResults = searchResult.SearchResults.OrderByDescending(a => a.ModifiedDate).ThenBy(b => b.Title).ThenByDescending(c => c.Rank).ToList();
                    }
                }
                else
                {
                    searchResult1.SearchResults = searchResult.SearchResults.OrderByDescending(a => a.Rank).ThenByDescending(b => b.ModifiedDate).ThenBy(c => c.Title).ToList();
                }
            }
            return searchResult1;
        }
    }
}
