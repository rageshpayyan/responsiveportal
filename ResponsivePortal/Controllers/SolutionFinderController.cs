using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using ResponsivePortal.Models;
using PortalAPI.Managers.Concrete;
using PortalAPI.Models;
using System.Web.Routing;
using System.Collections;
using Newtonsoft.Json;
using NLog;
using ResponsivePortal.Filters.MVC;
using ResponsivePortal;
using ResponsivePortal.Filters;
using System.Data.SqlClient;
using KBCommon.KBException;
using ResponsivePortal.Resources;
using System.Web.Helpers;

namespace ResponsivePortal.Controllers
{
    [ValidateInput(false)]
    [PortalConfigurationAction]
    [CustomErrorHandler]
    public class SolutionFinderController : Controller
    {
        private SolutionFinderManager _solFinderManager;
        private UsersManager _usersManager;
        private Portal _portal;
        private User _user;
        private ArticleManager _articleManager;
        private int clientId;
        private int portalId;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, string> Resources = new Dictionary<string, string>();
        public Dictionary<string, string> CommonResources = new Dictionary<string, string>();
        private Dictionary<string, string> ResourcesArticle = new Dictionary<string, string>();
        public HeaderViewModel headerVM;
        private string homeText;
        private string SFText;

        public SolutionFinderController(SolutionFinderManager _solFinderManager, ArticleManager articleManager, UsersManager usersManager)
        {
            this._solFinderManager = _solFinderManager;
            this._articleManager = articleManager;
            this._usersManager = usersManager;
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
            _user = HttpContext.Session.GetUser(portalId);
            SolutionFinderModule SFModule = (SolutionFinderModule)_portal.Configuration.SolutionFinderModule;
            if (SFModule == null)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(null, KBOp.LoadSolutionFinderPage, KBErrorHandler.GetMethodName(), GeneralResources.SolutionFinderModuleNotFoundError, LogEnabled.False);
                throw kbCustExp;
            }

            //Assign portal and user object to artilceManger
            this._solFinderManager.Portal = _portal;
            this._solFinderManager.User = HttpContext.Session.GetUser(portalId);
            //Assign portal and user object to artilceManger
            this._articleManager.Portal = _portal;
            this._articleManager.User = HttpContext.Session.GetUser(portalId);
            //resource object
            ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId, this._portal.PortalType, this._portal.Configuration, "solutionFinder");
            headerVM = ((CommonViewModel)ViewData["CommonViewModel"]).HeaderViewModel;
            Resources = Session.Resource(portalId, clientId, "solutionFinder", _portal.Language.Name);
            ResourcesArticle = Session.Resource(portalId, clientId, "ARTICLE", _portal.Language.Name);
            Resources = Resources.Concat(ResourcesArticle).ToDictionary(x => x.Key, x => x.Value);
            CommonResources = Session.Resource(portalId, clientId, "common", _portal.Language.Name);
            //get Module name and create navigation
            homeText = Utilities.getModuleText(headerVM, "home");
            SFText = Utilities.getModuleText(headerVM, "solutionFinder");
        }

        //
        // GET: /SolutionFinder/
        public ActionResult Index(string title)
        {
            try
            {
                //read and update clientid & portalid from routedata
                ReadDataFromRouteData();

                SolutionFinderModule SFModule = (SolutionFinderModule)_portal.Configuration.SolutionFinderModule;
                List<SolutionFinder> SFItems = SFModule.Children;

                // var ids = (from sf in SFItems select sf.Id).ToArray();
                //var commaSeparatedSolutionFinderIds = string.Join(",", ids);
                // var defaultSolutionFinders = _solFinderManager.GetListOfSolutionFindersId(commaSeparatedSolutionFinderIds);
                var defaultSolutionFinders = new List<SolutionFinder>();
                foreach (var item in SFItems)
                {
                    defaultSolutionFinders.Add(new SolutionFinder(item.SolutionFinderId, item.Name, item.ImageUrl, item.Description));
                }

                TempData["MainLayoutViewModel"] = Session["MainLayoutViewModel"];
                Session["SolutionFinderHistory"] = new List<HistoryViewModel>();
                SolutionFinderViewModel solutionFinderViewModel = new SolutionFinderViewModel();
                solutionFinderViewModel.portalId = portalId;
                solutionFinderViewModel.clientId = clientId;
                solutionFinderViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;
                solutionFinderViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
                solutionFinderViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
                solutionFinderViewModel.SolutionFinderTiles = new List<SolutionFinderTileViewModel>();
                // TODO : change MOCK for real
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();

                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = SFText, Value = "solutionFinder", Selected = true });
                solutionFinderViewModel.BreadcrumbViewModel = BreadcrumbViewModel;

                foreach (var solutionfinder in defaultSolutionFinders)
                {
                    SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel()
                    {
                        Title = solutionfinder.Name,
                        Content = Resources.ContainsKey("SOLUTIONFINDER_" + solutionfinder.SolutionFinderId + "_DESCRIPTION") ? Resources["SOLUTIONFINDER_" + solutionfinder.SolutionFinderId + "_DESCRIPTION"] : string.Empty,
                        Id = solutionfinder.SolutionFinderId,
                        Icon = solutionfinder.ImageUrl,
                        portalId = portalId,
                        clientId = clientId
                    };
                    solutionFinderViewModel.SolutionFinderTiles.Add(solutionFinderTileViewModel);
                }
                //setting resource objet to view model.
                solutionFinderViewModel.Resources = Resources;
                return View(solutionFinderViewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.SlnFinderIndex, KBErrorHandler.GetMethodName(), GeneralResources.IndexError,
                    new KBExceptionData("title", title));
                throw kbCustExp;
            }
        }

        public ActionResult GetSolutionFinderDetails(int solutionId)
        {
            int userid = 0;
            try
            {
                //read and update clientid & portalid from routedata
                ReadDataFromRouteData();
                
                if (_user != null && _user.UserId > 0)
                {
                    userid = _user.UserId;
                }
                this._solFinderManager.SFVisit(solutionId, portalId, clientId, userid, DateTime.Now, HttpContext.Session.SessionID, 0);

                SolutionFinderDetailsViewModel solutionFinderDetailsViewModel = new SolutionFinderDetailsViewModel();
                solutionFinderDetailsViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
                solutionFinderDetailsViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
                solutionFinderDetailsViewModel.portalId = portalId;
                solutionFinderDetailsViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.ImmediatePId = 0; // Parent Id, in this case its 0 (no parent)
                solutionFinderDetailsViewModel.ChoiceId = solutionId;
                solutionFinderDetailsViewModel.showBreadcrumb = _portal.Configuration.ShowBreadcrumbs;

                SolutionFinder objSF = GetSolutionFinder(solutionId);
                SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel();
                if (null != objSF)
                {
                    solutionFinderTileViewModel.Id = solutionId;
                    solutionFinderTileViewModel.Icon = objSF.ImageUrl.Replace('^', '\\');
                    solutionFinderTileViewModel.Title = objSF.Name;
                    solutionFinderTileViewModel.Content = Resources.ContainsKey("SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION") ? Resources["SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION"] : string.Empty;
                }
                solutionFinderTileViewModel.portalId = portalId;
                solutionFinderTileViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.SolutionFinderTileViewModel = solutionFinderTileViewModel;

                //----- Breadcrumbs ------
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = SFText, Value = "solutionFinder", Selected = false });

                solutionFinderDetailsViewModel.History = new List<HistoryViewModel>();
                Session["SolutionFinderHistory"] = new List<HistoryViewModel>();
                //setting resource objet to view model.
                solutionFinderDetailsViewModel.Resources = Resources;
                solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(solutionId, 0);

                BreadcrumbViewModel.NavigationList.Add(new SelectListItem()
                {
                    Text = solutionFinderTileViewModel.Title,
                    Value = solutionFinderDetailsViewModel.SolutionFinder == null ? solutionFinderTileViewModel.Id.ToString() : solutionFinderDetailsViewModel.SolutionFinder.SolutionFinderId.ToString(),
                    Selected = true
                });

                //---store question into tempdata
                TempData["question_" + _portal.PortalId.ToString() + "_" + solutionId.ToString()] = solutionFinderDetailsViewModel.SolutionFinder != null ? solutionFinderDetailsViewModel.SolutionFinder.Question : string.Empty;

                solutionFinderDetailsViewModel.BreadcrumbViewModel = BreadcrumbViewModel;                
                if (solutionFinderDetailsViewModel.SolutionFinder == null)
                {
                    //set empty model and handle it in UI
                    return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
                }                          
                
                //  UpdateSolutionFinderChoices(solutionFinderDetailsViewModel);
                return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
            }
            catch (JsonException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.JsonError,
                    new KBExceptionData("solutionId", solutionId));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("solutionId", solutionId));
                throw kbCustExp;
            }
        }
        public JsonResult ValidateRequestHeader()
        {
            string cookieToken = "";
            string formToken = "";

            try
            {
                IEnumerable<string> tokenHeaders;
                tokenHeaders = HttpContext.Request.Headers.GetValues("RequestVerificationToken");
                if (tokenHeaders != null)
                {
                    string[] tokens = tokenHeaders.First().Split(':');
                    if (tokens.Length == 2)
                    {
                        cookieToken = tokens[0].Trim();
                        formToken = tokens[1].Trim();
                    }
                }
                AntiForgery.Validate(cookieToken, formToken);
                return Json(new { Message = "success" });
            }
            catch (Exception ex)
            {
                return Json(new { Message = "false" });
            }
        }

        public ActionResult GetSolutionFinderDetailsById(string title, int sfid, int sfcid, bool fromwidget = false, int SearchId = 0, Boolean isSearch=false)
        {
            string icon = string.Empty;
            string sftitle = string.Empty;
            string content = string.Empty;
            int userid = 0;
            string searchText = string.Empty;
            try
            {
                //read and update clientid & portalid from routedata
                ReadDataFromRouteData();
                // Questions: id - always 0, I use sfcid as SolutionFinderId

                if (HttpContext.Session["SearchText_" + portalId.ToString()] != null && isSearch== true)
                {
                    searchText = (String)Session["SearchText_" + portalId.ToString()];
                }

                SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel();
                solutionFinderTileViewModel.portalId = portalId;
                solutionFinderTileViewModel.clientId = clientId;
                SolutionFinderDetailsViewModel solutionFinderDetailsViewModel = new SolutionFinderDetailsViewModel();
                solutionFinderDetailsViewModel.portalId = portalId;
                solutionFinderDetailsViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.FromWidget = fromwidget;
                solutionFinderDetailsViewModel.ImmediatePId = this._solFinderManager.GetSolutionFinderParentByChoice(sfcid); // Solution Finder Id
                solutionFinderDetailsViewModel.ChoiceId = sfcid;
                Session["SolutionFinderHistory"] = new List<HistoryViewModel>();
                solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(sfid, sfcid);
                solutionFinderDetailsViewModel.SolutionFinder.SolutionFinderId = sfid;
                // UpdateSolutionFinderChoices(solutionFinderDetailsViewModel);
                bool skipAdd = false;
                if ((solutionFinderDetailsViewModel.History != null)
                    && (solutionFinderDetailsViewModel.History.Count > 0)
                    && (solutionFinderDetailsViewModel.History[solutionFinderDetailsViewModel.History.Count - 1].SolutionFinderId == sfid))
                {
                    skipAdd = true;
                }
                if (!skipAdd)
                {
                    SolutionFinder objSF = GetSolutionFinder(sfid);
                    if (null != objSF)
                    {
                        icon = objSF.ImageUrl.Replace('^', '\\');
                        sftitle = objSF.Name;
                        content = Resources.ContainsKey("SOLUTIONFINDER_" + sfid.ToString() + "_DESCRIPTION") ? Resources["SOLUTIONFINDER_" + sfid.ToString() + "_DESCRIPTION"] : string.Empty;
                    }
                    solutionFinderDetailsViewModel.History = new List<HistoryViewModel>();
                }
                //----- Breadcrumbs ------
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                if (this._portal.Configuration.ShowBreadcrumbs)
                {
                    solutionFinderDetailsViewModel.showBreadcrumb = true;
                    BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = searchText, Value = "Search", Selected = false });
                    //searchResultFor
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem()
                    {
                        Text = sftitle,
                        Value = string.Empty,
                        Selected = true
                    });
                    solutionFinderDetailsViewModel.BreadcrumbViewModel = BreadcrumbViewModel;
                }
                solutionFinderDetailsViewModel.SolutionFinderTileViewModel = new SolutionFinderTileViewModel()
                {
                    Title = sftitle,
                    Id = sfid,
                    Content = content,
                    Icon = icon
                };
                solutionFinderDetailsViewModel.Resources = Resources;
                if (_user != null && _user.UserId > 0)
                {
                    userid = _user.UserId;
                }
                this._solFinderManager.SFVisit(sfid, portalId, clientId, userid, DateTime.Now, HttpContext.Session.SessionID, SearchId);

                return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
            }
            catch (JsonException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.JsonError,
                    new KBExceptionData("title", title), new KBExceptionData("sfid", sfid), new KBExceptionData("sfcid", sfcid));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("title", title), new KBExceptionData("sfid", sfid), new KBExceptionData("sfcid", sfcid));
                throw kbCustExp;
            }
        }

        [HttpPost]
        public ActionResult GetSolutionFinderDetailsByIdWithParams(int parentsolutionId, int solutionId, int choiceId, bool nav, string answer)
        {
            bool navigation = false;
            int ImmediatePId;
            string solFinderName = string.Empty;
            string question = string.Empty;

            try
            {
                if (nav) navigation = true;
                //read and update clientid & portalid from routedata  
                ReadDataFromRouteData();
                SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel();
                SolutionFinderDetailsViewModel solutionFinderDetailsViewModel = new SolutionFinderDetailsViewModel();
                solutionFinderTileViewModel.portalId = portalId;
                solutionFinderTileViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.portalId = portalId;
                solutionFinderDetailsViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.ChoiceId = choiceId; // Choice Id
                question = TempData["question_" + _portal.PortalId.ToString() + "_" + parentsolutionId.ToString()] != null ? TempData["question_" + _portal.PortalId.ToString() + "_" + parentsolutionId.ToString()].ToString() : string.Empty;
                ImmediatePId = this._solFinderManager.GetSolutionFinderParentByChoice(choiceId); // Solution Finder Id
                solutionFinderDetailsViewModel.ImmediatePId = ImmediatePId;
                if (nav)
                {
                    if (ImmediatePId > 0)
                    {
                        solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(ImmediatePId, choiceId);
                    }
                    else
                    {
                        solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(parentsolutionId, 0);
                    }
                }
                else
                {
                    solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(ImmediatePId, choiceId);
                }

                if (!navigation)
                {
                    solutionFinderDetailsViewModel.History = (List<HistoryViewModel>)Session["SolutionFinderHistory"];
                    bool skipAdd = false;
                    if ((solutionFinderDetailsViewModel.History != null)
                        && (solutionFinderDetailsViewModel.History.Count > 0)
                        && (solutionFinderDetailsViewModel.History[solutionFinderDetailsViewModel.History.Count - 1].SolutionFinderId == choiceId)
                       )
                    {
                        skipAdd = true;
                    }
                    if (!skipAdd)
                    {
                        HistoryViewModel SV = solutionFinderDetailsViewModel.History.FirstOrDefault(Q => Q.ChoiceId == choiceId);
                        if (SV == null)
                        {
                            solutionFinderDetailsViewModel.History.Add(new HistoryViewModel()
                                {
                                    SolutionFinderId = solutionId,
                                    Question = Utilities.GetHistoryTextForSolutionFinder(question),
                                    Answer = answer,
                                    ChoiceId = parentsolutionId
                                });
                        }
                        Session["SolutionFinderHistory"] = solutionFinderDetailsViewModel.History;

                    }
                }
                else
                {
                    solutionFinderDetailsViewModel.History = (List<HistoryViewModel>)Session["SolutionFinderHistory"];
                    int chIndex = solutionFinderDetailsViewModel.History.FindIndex(x => x.ChoiceId == choiceId);
                    //chIndex = chIndex + 1;
                    solutionFinderDetailsViewModel.History.RemoveRange(chIndex, (solutionFinderDetailsViewModel.History.Count) - chIndex);
                    Session["SolutionFinderHistory"] = solutionFinderDetailsViewModel.History;

                }

                //-------store question to tempdata
                TempData["question_" + _portal.PortalId.ToString() + "_" + choiceId.ToString()] = solutionFinderDetailsViewModel.SolutionFinder != null ? solutionFinderDetailsViewModel.SolutionFinder.Question : string.Empty;

                //-------Solution FInder Name
                SolutionFinder objSF = GetSolutionFinder(solutionId);
                if (null != objSF)
                {
                    solFinderName = objSF.Name;
                }
                //----- Breadcrumbs ------
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                solutionFinderDetailsViewModel.showBreadcrumb = this._portal.Configuration.ShowBreadcrumbs;
                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = SFText, Value = "solutionFinder", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem()
                {
                    Text = solFinderName,
                    Value = string.Empty,
                    Selected = true
                });
                solutionFinderDetailsViewModel.BreadcrumbViewModel = BreadcrumbViewModel;
                solutionFinderDetailsViewModel.SolutionFinderTileViewModel = new SolutionFinderTileViewModel()
                {
                    Title = solFinderName,
                    Id = solutionId,
                };
                solutionFinderDetailsViewModel.Resources = Resources;
                return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
            }
            catch (JsonException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.JsonError,
                    new KBExceptionData("solutionId", solutionId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("solutionId", solutionId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
        }

        private ActionResult GetSFViewDetailsWithArticleById(int parentsolutionId, int solutionId, int choiceId, int articleId)
        {
            string solFinderName = string.Empty;
            string description = string.Empty;
            string icon = string.Empty;
            try
            {
                //read and update clientid & portalid from routedata
                ReadDataFromRouteData();
                SolutionFinderDetailsViewModel solutionFinderDetailsViewModel = new SolutionFinderDetailsViewModel();
                SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel();
                solutionFinderDetailsViewModel.portalId = portalId;
                solutionFinderDetailsViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.ImmediatePId = parentsolutionId; // Solution Finder Id;
                solutionFinderDetailsViewModel.ChoiceId = choiceId;
                solutionFinderTileViewModel.clientId = clientId;
                solutionFinderTileViewModel.portalId = portalId;
                solutionFinderTileViewModel.Id = parentsolutionId;
                solFinderName = GetSolutionFinderName(solutionId);

                //-------Solution FInder Name
                if (parentsolutionId == 0)
                {
                    SolutionFinder objSF = GetSolutionFinder(solutionId);
                    if (null != objSF)
                    {
                        solutionFinderTileViewModel.Id = solutionId;
                        solutionFinderTileViewModel.Icon = objSF.ImageUrl.Replace('^', '\\');
                        solutionFinderTileViewModel.Title = objSF.Name;
                        solutionFinderTileViewModel.Content = Resources.ContainsKey("SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION") ? Resources["SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION"] : string.Empty;
                    }                    
                }
                else
                {
                    solutionFinderTileViewModel.Id = solutionId;
                    solutionFinderTileViewModel.Title = solFinderName;
                }
                solutionFinderDetailsViewModel.SolutionFinderTileViewModel = solutionFinderTileViewModel;
                if (parentsolutionId==0)
                {
                    solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(solutionId, 0);
                }
                else
                {
                    solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(solutionId, choiceId);
                }
                
                //UpdateSolutionFinderChoices(solutionFinderDetailsViewModel);
                solutionFinderDetailsViewModel.History = (List<HistoryViewModel>)Session["SolutionFinderHistory"];

                //----- Breadcrumbs ------
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                solutionFinderDetailsViewModel.showBreadcrumb = this._portal.Configuration.ShowBreadcrumbs;

                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = SFText, Value = "solutionFinder", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem()
                {
                    Text = solutionFinderTileViewModel.Title,
                    Value = string.Empty,
                    Selected = true
                });
                solutionFinderDetailsViewModel.BreadcrumbViewModel = BreadcrumbViewModel;

                //read and update clientid & portalid from routedata
                if (articleId > 0)
                {
                    var articleItem = (ArticleItem)_articleManager.GetArticleItemById(articleId, HttpContext.Session.SessionID, "", 0);
                    if (articleItem != null)
                    {
                        if (articleItem.Extension == ".html" || articleItem.Extension == ".htm")
                        {
                            articleItem.Content.CompleteContent = Utilities.UpdateArticleURlInArticleContent(articleItem.Content.CompleteContent, "/articleRedirect.aspx?aid=", "/Article/Index/" + clientId + "/" + portalId + "?id=");
                        }
                    }

                    //split artilce content
                    Utilities.splitArticleContent(ref articleItem);

                    //overwrite kb name with XML name
                    string kbname = GetKnowledgebaseName(articleItem.KnowledgeBase.Id);
                    if (!string.IsNullOrEmpty(kbname))
                    {
                        articleItem.KnowledgeBase.Name = kbname;
                    }

                    string adminURL = _articleManager.GetAdminURL(); // get admin URL from DB

                    Dictionary<string, string> resources = Session.Resource(portalId, clientId, "ARTICLE", _portal.Language.Name);
                    ArticlePartialViewModel articlePartialViewModel = ResponsivePortal.Utilities.CreateArticlePartialViewModel(articleItem,
                            (ArticleModule)_portal.Configuration.ArticlesModule,
                             GetListOfNodeName(articleItem.Categories), GetListOfNodeName(articleItem.Attributes),
                             this._portal.PortalType, resources, clientId, portalId, adminURL);
                    solutionFinderDetailsViewModel.ArticlePartialViewModel = articlePartialViewModel;
                    string shareUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                    shareUrl = shareUrl + "/Article/Index/" + clientId + "/" + portalId + "/?id=" + articleId;
                    solutionFinderDetailsViewModel.ArticlePartialViewModel.ShareItem = Utilities.GetShareViewModel(articleItem.ArticleId, articleItem.Title,
                        shareUrl, this._articleManager.GetEmailArticleContent().Replace("[[articleurl]]", shareUrl),
                        solutionFinderDetailsViewModel.ArticlePartialViewModel.ArticleConfiguration.articleShareProperties,
                        resources, clientId, portalId);
                   
                }
                solutionFinderDetailsViewModel.Resources = Resources;

                if (articleId > 0)
                { solutionFinderDetailsViewModel.ArticlePartialViewModel.Resources = Resources; }
                return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
            }
            catch (JsonException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.JsonError,
                    new KBExceptionData("info", ""), new KBExceptionData("solutionId", solutionId), new KBExceptionData("articleId", articleId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("info", ""), new KBExceptionData("solutionId", solutionId), new KBExceptionData("articleId", articleId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
        }

        public ActionResult GetSolutionFinderDetailsWithArticleByID(int parentsolutionId, int solutionId, int choiceId, int articleId)
        {
            return GetSFViewDetailsWithArticleById(parentsolutionId, solutionId, choiceId, articleId);
        }

        [HttpPost]
        public ActionResult GetSolutionFinderDetailsWithArticleByID()
        {
            int parentsolutionId=0, solutionId=0, choiceId=0, articleId =0;
            try
            {
                parentsolutionId = Convert.ToInt32(Request.Form["parentsolutionId"].ToString());
                solutionId = Convert.ToInt32(Request.Form["solutionId"].ToString());
                choiceId = Convert.ToInt32(Request.Form["choiceId"].ToString());
                articleId = Convert.ToInt32(Request.Form["articleId"].ToString());
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("parentsolutionId", parentsolutionId), new KBExceptionData("solutionId", solutionId), new KBExceptionData("choiceId", choiceId), new KBExceptionData("articleId", articleId));
                throw kbCustExp;
            }

            return GetSFViewDetailsWithArticleById(parentsolutionId, solutionId, choiceId, articleId);
        }

        [HttpPost]
        public ActionResult GetPreviousSolutionFinderDetailsByIdWithParams(int parentsolutionId, int solutionId, int choiceId, string answer)
        {
            string solFinderName = string.Empty;
            try
            {
                //read and update clientid & portalid from routedata  
                ReadDataFromRouteData();

                SolutionFinderDetailsViewModel solutionFinderDetailsViewModel = new SolutionFinderDetailsViewModel();
                solutionFinderDetailsViewModel.portalId = portalId;
                solutionFinderDetailsViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.ChoiceId = parentsolutionId;
                //if (parentsolutionId != choiceId)
                //{
                solutionFinderDetailsViewModel.ImmediatePId = this._solFinderManager.GetSolutionFinderParentByChoice(parentsolutionId); // Solution Finder Id 
                //}
                solutionFinderDetailsViewModel.SolutionFinder = (PortalAPI.Models.SolutionFinderChoice)this._solFinderManager.GetSolutionFinderChoiceById(0, parentsolutionId);//0,solutionId);
                if (solutionFinderDetailsViewModel.SolutionFinder != null)
                {
                    TempData["question_" + _portal.PortalId.ToString() + "_" + parentsolutionId.ToString()] = solutionFinderDetailsViewModel.SolutionFinder.Question != null ? solutionFinderDetailsViewModel.SolutionFinder.Question : string.Empty;
                }
                // UpdateSolutionFinderChoices(solutionFinderDetailsViewModel);

                solutionFinderDetailsViewModel.History = (List<HistoryViewModel>)Session["SolutionFinderHistory"];
                if ((solutionFinderDetailsViewModel.History != null)
                    && (solutionFinderDetailsViewModel.History.Count > 0))
                {
                    int count = solutionFinderDetailsViewModel.History.Count;
                    solutionFinderDetailsViewModel.History.RemoveAt(count - 1);
                }
                string icon = string.Empty;
                SolutionFinderTileViewModel solutionFinderTileViewModel = new SolutionFinderTileViewModel();

                solFinderName = GetSolutionFinderName(solutionId);

                if (solutionFinderDetailsViewModel.ImmediatePId == 0)
                {
                    SolutionFinder objSF = GetSolutionFinder(solutionId);
                    if (null != objSF)
                    {

                        solutionFinderTileViewModel.Id = solutionId;
                        solutionFinderTileViewModel.Title = objSF.Name;
                        solutionFinderTileViewModel.Icon = objSF.ImageUrl;
                        solutionFinderTileViewModel.Content = objSF.Description;
                        solutionFinderTileViewModel.Content = Resources.ContainsKey("SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION") ? Resources["SOLUTIONFINDER_" + solutionId.ToString() + "_DESCRIPTION"] : string.Empty;
                    }
                }
                else
                {
                    solutionFinderTileViewModel.Id = solutionId;
                    solutionFinderTileViewModel.Title = solFinderName;
                }

                solutionFinderTileViewModel.portalId = portalId;
                solutionFinderTileViewModel.clientId = clientId;
                solutionFinderDetailsViewModel.SolutionFinderTileViewModel = solutionFinderTileViewModel;
                //----- Breadcrumbs ------
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                solutionFinderDetailsViewModel.showBreadcrumb = this._portal.Configuration.ShowBreadcrumbs;
                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = SFText, Value = "solutionFinder", Selected = false });
                BreadcrumbViewModel.NavigationList.Add(new SelectListItem()
                {
                    Text = solFinderName,
                    Value = string.Empty,
                    Selected = true
                });
                solutionFinderDetailsViewModel.BreadcrumbViewModel = BreadcrumbViewModel;

                solutionFinderDetailsViewModel.Resources = Resources;
                return View("SolutionFinderDetails", solutionFinderDetailsViewModel);
            }
            catch (JsonException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.JsonError,
                    new KBExceptionData("solutionId", solutionId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetSlnFinderDetails, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError,
                    new KBExceptionData("solutionId", solutionId), new KBExceptionData("solFinderName", solFinderName));
                throw kbCustExp;
            }
        }

        private void UpdateSolutionFinderChoices(SolutionFinderDetailsViewModel solutionFinderDetailsVM)
        {
            if ((solutionFinderDetailsVM.SolutionFinder == null) || (solutionFinderDetailsVM.SolutionFinder.Choices == null) || (solutionFinderDetailsVM.SolutionFinder.Choices.Count == 0))
            {
                return;
            }
            bool found = false;
            int count = solutionFinderDetailsVM.SolutionFinder.Choices.Count - 1;
            for (int i = count; i >= 0; i--)
            {
                SolutionFinderChoice choice = solutionFinderDetailsVM.SolutionFinder.Choices[i];
                if ((choice.ArticleCount == 0) && (choice.ChildrenCount == 0))
                {
                    // solutionFinderDetailsVM.SolutionFinder.Choices.Remove(choice);
                }
                else
                {
                    found = true;
                }
            }
            if (!found)
            {
                solutionFinderDetailsVM.NoChoicesMessage = "No Choices";
                solutionFinderDetailsVM.SolutionFinder.Choices = new List<SolutionFinderChoice>();
            }
        }

        private string RenderRazorViewToString(ControllerContext context, string viewName, object viewData)
        {
            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var vdd = new ViewDataDictionary(viewData);
                var viewCxt = new ViewContext(context, viewResult.View, vdd, new TempDataDictionary(), sw);
                viewResult.View.Render(viewCxt, sw);
                return sw.GetStringBuilder().ToString();
            }
        }

        private ArrayList GetListOfNodeName(List<ITree> trees)
        {
            ArrayList result = new ArrayList();
            foreach (ITree t in trees)
            {
                var names = new List<string>();
                foreach (Node n in t.Nodes)
                {
                    Queue<Node> qnode = new Queue<Node>();
                    qnode.Enqueue(n);
                    while (qnode.Count > 0)
                    {
                        Node frontNode = qnode.Dequeue();
                        names.Add(frontNode.Name);
                        if (frontNode.Children != null)
                        {
                            foreach (Node childNodes in frontNode.Children)
                            {
                                qnode.Enqueue(childNodes);
                            }
                        }
                    }
                }
                result.Add(names);
            }
            return result;
        }

        private string GetSolutionFinderName(int sfid)
        {
            string sFinderName = string.Empty;
            if (sfid > 0)
            {
                SolutionFinderModule SFModule = (SolutionFinderModule)_portal.Configuration.SolutionFinderModule;
                List<SolutionFinder> SFItems = SFModule.Children;

                SolutionFinder SF = SFItems.FirstOrDefault(Q => Q.SolutionFinderId == sfid);
                if (SF != null)
                {
                    sFinderName = SF.Name;
                }

            }
            return sFinderName;
        }

        private SolutionFinder GetSolutionFinder(int sfid)
        {
            string sFinderName = string.Empty;
            if (sfid > 0)
            {
                SolutionFinderModule SFModule = (SolutionFinderModule)_portal.Configuration.SolutionFinderModule;
                List<SolutionFinder> SFItems = SFModule.Children;

                SolutionFinder SF = SFItems.FirstOrDefault(Q => Q.SolutionFinderId == sfid);
                if (SF != null)
                {
                    return SF;
                }

            }
            return null;
        }

        private string GetKnowledgebaseName(int kbid)
        {
            string kbname = string.Empty;
            try
            {
                //get knolwlegebase name from configuration file
                KnowledgeBase KB = _portal.Knowledgebases.FirstOrDefault(Q => Q.Id == kbid);
                if (KB != null)
                {
                    kbname = KB.Name;
                }
                return kbname;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.SlnFinderIndex, KBErrorHandler.GetMethodName(), GeneralResources.GetSlnFinderDetailsError, new KBExceptionData("kbid", kbid));
                throw kbCustExp;
            }
        }
    }
}