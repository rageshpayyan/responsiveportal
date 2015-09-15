using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ResponsivePortal.Models;
using PortalAPI.Managers.Concrete;
using System.Collections;
using PortalAPI.Models;
using NLog;
using ResponsivePortal.Filters.MVC;
using ResponsivePortal.Filters;
using System.Text.RegularExpressions;
using KBCommon.KBException;
using ResponsivePortal.Resources;
namespace ResponsivePortal.Controllers
{
    [PortalConfigurationAction]
    [CustomErrorHandler]
    public class ArticleController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private ArticleManager _articleManager;
        private int clientId;
        private int portalId;
        private Portal _portal;
        private User _user;
        private Dictionary<string, string> resources;
        public Dictionary<string, string> CommonResources = new Dictionary<string, string>();
        public HeaderViewModel headerVM;
        public ArticleController(ArticleManager articleManager)
        {
            this._articleManager = articleManager;
        }
        //
        // GET: /Articles/
        public ActionResult Index(int? id, int? relparticId, string relpname, int catId = 0, Boolean fromwidget = false, int searchid = 0, Boolean isSearch = false)
        {
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();

                string searchText = string.Empty;
                ArticleViewModel articlesViewModel = new ArticleViewModel();

                if (HttpContext.Session["SearchText_" + portalId.ToString()] != null && isSearch == true)
                {
                    searchText = (String)Session["SearchText_" + portalId.ToString()];
                }

                var articleId = 0;
                if (string.IsNullOrEmpty(Request.QueryString["id"]) == false)
                {
                    if (Request.QueryString["id"].ToString().Contains("|"))
                    {
                        articleId = Int32.Parse(Request.QueryString["id"].ToString().Replace("|", string.Empty).Trim());
                    }
                    else articleId = int.Parse(Request.QueryString["id"]);
                }
                if (!(id.HasValue) && !(relparticId.HasValue))
                {
                    ViewData.Add("ArticleNotFound", "ArticleID is not passed!");
                    return View();
                }
                if (id.Value.ToString().Contains("|"))
                {
                    articleId = Int32.Parse(id.Value.ToString().Replace("|", string.Empty).Trim());
                }
                else articleId = id.Value;
                var articleItem = _articleManager.GetArticleItemById(articleId, HttpContext.Session.SessionID, searchText, searchid);

                if (articleItem == null)
                {
                    ViewData.Add("ArticleNotFound", "ArticleID not found!");
                    return View();
                }
                if (articleItem.Extension == ".html" || articleItem.Extension == ".htm")
                {
                    if (articleItem.Content.CompleteContent.Contains("articleRedirect.aspx"))
                        articleItem.Content.CompleteContent = Utilities.UpdateArticleURlInArticleContent(articleItem.Content.CompleteContent, "/articleRedirect.aspx?aid=", "/Article/Index/" + clientId + "/" + portalId + "?id=");
                }

                //split artilce content
                Utilities.splitArticleContent(ref articleItem);


                articlesViewModel.clientId = clientId;
                articlesViewModel.portalId = portalId;
                articlesViewModel.showBreadcrumb = this._portal.Configuration.ShowBreadcrumbs;
                articlesViewModel.SessionTimeOutWarning = Utilities.GetResourceText(CommonResources, "SESSIONTIMEOUTWARNING");
                articlesViewModel.SessionTimedOut = Utilities.GetResourceText(CommonResources, "SESSIONTIMEDOUT");
                articlesViewModel.FromWidget = fromwidget;
                //overwrite kb name with XML name
                string kbname = getKnowledgebaseName(articleItem.KnowledgeBase.Id);
                if (!string.IsNullOrEmpty(kbname))
                {
                    articleItem.KnowledgeBase.Name = kbname;
                }

                string adminURL = _articleManager.GetAdminURL(); // get admin URL from DB

                articlesViewModel.ArticlePartialViewModel = ResponsivePortal.Utilities.CreateArticlePartialViewModel(articleItem,
                            (ArticleModule)_portal.Configuration.ArticlesModule,
                             GetListOfNodeName(articleItem.Categories), GetListOfNodeName(articleItem.Attributes),
                             this._portal.PortalType, this.resources, clientId, portalId, adminURL);
                articlesViewModel.ArticleId = articleId;
                //get Module name and create navigation
                string homeText = Utilities.getModuleText(headerVM, "home");

                // Uri xx = Request.UrlReferrer;
                // id - is Article Id
                // Create MOCK: remove for real
                BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                if (this._portal.Configuration.ShowBreadcrumbs)
                {
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = homeText, Value = "home", Selected = false });
                    if (!String.IsNullOrEmpty(searchText))
                    {
                        BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = searchText, Value = "Search", Selected = false });
                    }
                    if (!String.IsNullOrEmpty(relpname)) // Selected related artilce Title 
                    {
                        BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = relpname, Value = "article", Selected = false });
                        articlesViewModel.RelativeArtilceParentId = relparticId; // Selected related artilce Id
                    }
                    if (catId != 0)
                    {
                        BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = catId.ToString(), Value = "Browse", Selected = false });
                    }
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = articleItem.Title, Value = "", Selected = true });
                }
                articlesViewModel.BreadcrumbViewModel = BreadcrumbViewModel;
                articlesViewModel.SearchText = searchText;
                //this will contain arrayListof Attribute names. eg. "Apple,Iphone 5...

                articlesViewModel.RelAnsweres = articleItem.RelatedArticles;
                articlesViewModel.RelLinks = articleItem.RelatedLinks;

                articlesViewModel.Resources = resources;
                string shareUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                shareUrl = shareUrl + "/Article/Index/" + clientId + "/" + portalId + "/?id=" + articleId;
                articlesViewModel.ArticlePartialViewModel.ShareItem = Utilities.GetShareViewModel(articleItem.ArticleId, articleItem.Title,
                    shareUrl, this._articleManager.GetEmailArticleContent().Replace("[[articleurl]]", shareUrl),
                    articlesViewModel.ArticlePartialViewModel.ArticleConfiguration.articleShareProperties,
                    resources, clientId, portalId);
                //GetShareViewModel(articleItem.ArticleId, articleItem.Title, 
                // articlesViewModel.ArticlePartialViewModel.ArticleConfiguration.articleShareProperties);

                return View(articlesViewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ArticleIndex, KBErrorHandler.GetMethodName(), GeneralResources.ArticleIndexError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)), new KBExceptionData("relparticId", (relparticId.HasValue ? relparticId.Value : 0)),
                    new KBExceptionData("relpname", relpname), new KBExceptionData("catId", catId));
                throw kbCustExp;
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
            _user = HttpContext.Session.GetUser(portalId);
            _portal = Session.GetPortalSessions().GetPortalSession(portalId, clientId).Portal;
            _articleManager.Portal = _portal;
            _articleManager.User = _user;
            resources = Session.Resource(portalId, clientId, "ARTICLE", _portal.Language.Name);
            CommonResources = Session.Resource(portalId, clientId, "common", _portal.Language.Name);
            ViewData["CommonViewModel"] = Utilities.CreateCommonViewModel(clientId, portalId, this._portal.PortalType, this._portal.Configuration, "article");
            headerVM = ((CommonViewModel)ViewData["CommonViewModel"]).HeaderViewModel;
        }

        [ValidateInput(false)]
        public ActionResult ToggleArticleSubscription(int? id, string email, Boolean isSubscribe=true)
        {
            Boolean togglesubscription = false;
            int userid = -1;
            string registeredemail = String.Empty;

            if (!String.IsNullOrEmpty(email))
                registeredemail = email;
            //  string newemail = "Default@moxiesoft.com";
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                if (_user != null && _user.UserId > 0)
                {
                    userid = _user.UserId;
                    registeredemail = String.IsNullOrEmpty(registeredemail) ? _user.Email : registeredemail;
                }

                togglesubscription = _articleManager.SetArticleSubscription(userid, Convert.ToInt32(id), registeredemail, isSubscribe);

                if (!(togglesubscription) && !(isSubscribe))
                {
                    return Json(new { Message = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBE_NOTSUBSCRIBED") }, JsonRequestBehavior.AllowGet);
                }

                if (!(togglesubscription) && (isSubscribe))
                {
                    return Json(new { Message = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBE_SUBSCRIBED") }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { Message = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBE_CONFIRMATIONLABEL") }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                KBCustomException.ProcessException(ex, KBOp.UpdateArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleSubscriptionError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)), new KBExceptionData("email", email), new KBExceptionData("isSubscribe", isSubscribe));
                return Json(new { Message = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBE_NOTSUBSCRIBED") }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult FavoriteArticle(int? id)
        {
            int result;
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                result = _articleManager.SetFavoriteArticle(Convert.ToInt32(id), Convert.ToString(HttpContext.Session.SessionID));
                if (result == -1)
                {
                    return Json(new { Result = result, Message = Utilities.GetResourceText(resources, "CONTROLS_FAVORITE_EXCEEDED") });
                }
                return Json(new { Result = result, Message = Utilities.GetResourceText(resources, "CONTROLS_FAVORITE_CONFIRMATIONLABEL") });
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.UpdateArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleSetFavError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)));
                throw kbCustExp;
            }
        }
        public ActionResult Attachment(int? attachmentid, string filename, string extension)
        {
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();

                System.IO.MemoryStream s = _articleManager.GetResourceFileContent(attachmentid.ToString(), extension);
                byte[] bts = new byte[s.Length];
                s.Read(bts, 0, bts.Length);
                return File(bts, System.Net.Mime.MediaTypeNames.Application.Octet, filename + extension);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleGetResourceError,
                    new KBExceptionData("attachmentid", (attachmentid.HasValue ? attachmentid.Value : 0)), new KBExceptionData("filename", filename), new KBExceptionData("extension", extension));
                throw kbCustExp;
            }
        }

        public ActionResult Download(int articleid)
        {
            int kbid = 0;
            string filename = string.Empty;
            string extension = string.Empty;
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                ArticleItem artItem = _articleManager.GetArticleItemById(articleid, null, null, 0);
                kbid = artItem.KnowledgeBase.Id;
                filename = artItem.Title;
                extension = artItem.Extension;

                System.IO.MemoryStream s = _articleManager.GetArticleFileContent(articleid, kbid, extension);
                if(s ==null)
                {
                    string userErrorMsg = null;
                    userErrorMsg = GeneralResources.UnAuthorizedAccess;
                    UnauthorizedAccessException unauthedAccessEx = new UnauthorizedAccessException(GeneralResources.UnAuthorizedAccess);
                    KBCustomException kbCustExp = KBCustomException.ProcessException(unauthedAccessEx, KBOp.RepositorySearch, KBErrorHandler.GetMethodName(), userErrorMsg,
                        new KBExceptionData("filename", filename), new KBExceptionData("extension", extension));
                    throw kbCustExp;
                }
                byte[] bts = new byte[s.Length];
                s.Read(bts, 0, bts.Length);
                return File(bts, System.Net.Mime.MediaTypeNames.Application.Octet, filename + extension);
            }
            catch (Exception ex)
            {

                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleFileContentError,
                    new KBExceptionData("articleid", articleid), new KBExceptionData("kbid", kbid), new KBExceptionData("filename", filename), new KBExceptionData("extension", extension));
                throw kbCustExp;
            }
        }
        
        [ValidateInput(false)]
        public JsonResult InsertArticleRating(int? id, bool positive)
        {
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                bool result = _articleManager.InsertArticleRating(Convert.ToInt32(id), positive);

                if (!positive && _user != null)
                {
                    return Json(new { Result = result, Name = _user.LoginName, Email = _user.Email }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { Result = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.UpdateArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleSetRatingError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)), new KBExceptionData("positive", positive));
                throw kbCustExp;
            }

        }

        public JsonResult InsertArticleFeedback(int? id, string feedback, string name, string email)
        {
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                bool result = _articleManager.InsertArticleFeedBack(Convert.ToInt32(id), feedback, name, email);

                return Json(new { Result = result, Message = Utilities.GetResourceText(resources, "CONTROLS_FEEDBACK_CONFIRMATIONLABEL") });
            }
            catch (Exception ex)
            {
                KBCustomException.ProcessException(ex, KBOp.UpdateArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleSetFeedbackError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)), new KBExceptionData("feedback", feedback));
                return Json(new { Result = false, Message = "Failed to send Email!!" });
            }
        }

        public JsonResult ShareSendEmail(int? id, string email, string subject, string body)
        {
            try
            {
                //read portalid and clientid from route data
                ReadDataFromRouteData();
                bool result = _articleManager.ShareSendEmail(Convert.ToInt32(id), email, subject, body);

                return Json(new { Result = result, Message = Utilities.GetResourceText(resources, "CONTROLS_SHARE_EMAIL_CONFIRMATIONLABEL") });
            }
            catch (Exception ex)
            {
                KBCustomException.ProcessException(ex, KBOp.UpdateArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleSendEmailError,
                    new KBExceptionData("id", (id.HasValue ? id.Value : 0)), new KBExceptionData("email", email), new KBExceptionData("subject", subject), new KBExceptionData("body", body));
                return Json(new { Result = false, Message = "Failed to send Email!!" });
            }
        }

        private ArrayList GetListOfNodeName(List<ITree> trees)
        {
            try
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
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleGetListError);
                throw kbCustExp;
            }
        }

        private string getKnowledgebaseName(int kbid)
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
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetArticle, KBErrorHandler.GetMethodName(), GeneralResources.ArticleGetKbName,
                    new KBExceptionData("kbid", kbid));
                throw kbCustExp;
            }
        }
    }
}