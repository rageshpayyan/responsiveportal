using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using ResponsivePortal.Models;
using PortalAPI.Managers.Concrete;
using ResponsivePortal.Filters.MVC;
using PortalAPI.Models;
using NLog;
using System.Xml;
using System.IO;
using ResponsivePortal.Filters;
using KBCommon.KBException;
using ResponsivePortal.Resources;

namespace ResponsivePortal.Controllers
{
     [CustomErrorHandler]
    public class AdminController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private UsersManager _usersManager;
        private AdminManager _adminManager;
        public AdminController(UsersManager usersManager, AdminManager adminManager)
        {
            this._usersManager = usersManager;
            this._adminManager = adminManager;
        }
        public AdminController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        { }
        public AdminController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }
         
        public UserManager<ApplicationUser> UserManager { get; private set; }
        public string DataConfigPath
        {
            get { return Path.Combine(_adminManager.DataPath, "knowledgebase\\PortalConfiguration"); }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        public ActionResult Index()
        {
            try
            {
                ViewBag.Title = "log in";
                var user = (PortalAPI.Models.AdminUser)Session["AdminUserSession"];
                if (user != null)
                {
                    return RedirectToAction("PortalList", "Admin");
                }
                return View();
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminActivity, KBErrorHandler.GetMethodName(), GeneralResources.LoginError);
                throw kbCustExp;
            }
        }
        [HttpPost]
         [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(AdminLoginViewModel model, string returnUrl, int clientId)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    PortalAPI.Models.AdminUser user = _usersManager.AdminLogin(model.UserName, model.Password);
                    if (user != null)
                    {
                        if (user.UserId != 0)
                        {
                            Session["AdminUserSession"] = user;
                            int clientid = _usersManager.GetClientId(user.UserId);
                            Session["ClientId"] = clientid;
                            var appUser = new ApplicationUser() { UserName = user.LoginName };
                            return RedirectToAction("PortalList", "Admin");
                        }
                        else
                        {
                            //ModelState.AddModelError("", user.ErrMsg);
                            ViewBag.ErrMsg = user.ErrMsg;
                        }
                    }
                    else
                    {
                        //ModelState.AddModelError("", "Login incorrect. Please try again.");
                        ViewBag.ErrMsg = "Login incorrect. Please try again.";
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminActivity, KBErrorHandler.GetMethodName(), GeneralResources.AdminLogin,
                    new KBExceptionData("model.UserName", model.UserName), new KBExceptionData("returnUrl", returnUrl), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////    
        public ActionResult PortalList()
        {
            int clientId = 0;
            AdminUser user = null;
            try
            {
                user = (PortalAPI.Models.AdminUser)Session["AdminUserSession"];
                if (user != null)
                {
                    var cid = Session["ClientId"];
                    clientId = Convert.ToInt32(cid.ToString());
                    ViewBag.UserName = _adminManager.GetUserName(user.UserId, clientId);
                    if (ModelState.IsValid)
                    {
                        PortalViewModel viewmodel = new PortalViewModel();

                        viewmodel.PortalPS4ViewModel = _adminManager.GetPS4Portals(clientId);
                        viewmodel.PortalPS5ViewModel = _adminManager.GetConvertedPortals(clientId);

                        return View(viewmodel);
                    }
                }
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminCreds, KBErrorHandler.GetMethodName(), GeneralResources.AdminGetUserName,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1));
                throw kbCustExp;
            }
        }

        [HttpPost]
         [ValidateAntiForgeryToken]
        public async Task<ActionResult> PortalList(PortalViewModel viewmodel)
        {
            int clientId = 0;
            AdminUser user = null;
            try
            {
                var cid = Session["ClientId"];
                clientId = Convert.ToInt32(cid.ToString());
                user = (PortalAPI.Models.AdminUser)Session["AdminUserSession"];
                string name = Request.Form["listbox"];
                int portalId = Convert.ToInt32(name);
                var addportal = _adminManager.AddNewPortal(portalId, clientId);                
                var lang = CreateLangDictionary();               
                var createfolders = _adminManager.CreateConfigurationFolders(portalId, clientId, "");
                viewmodel.PortalPS4ViewModel = _adminManager.GetPS4Portals(clientId);
                viewmodel.PortalPS5ViewModel = _adminManager.GetConvertedPortals(clientId);
                ViewBag.UserName = _adminManager.GetUserName(user.UserId, clientId);
                return View(viewmodel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminAddPortal, KBErrorHandler.GetMethodName(), GeneralResources.AdminGetPortals,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1));
                throw kbCustExp;
            }
        }
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ActionResult Edit(string message, string msgType)
        {
            AdminUser user = null;
            try
            {
                ViewBag.Title = "Edit Configuration";
                ViewBag.Message = message;
                ViewBag.Msgtype = msgType;
                user = (PortalAPI.Models.AdminUser)Session["AdminUserSession"];
                if (user != null)
                {
                    var portId = RouteData.Values["portalId"];
                    var portalId = Convert.ToInt32(portId);
                    
                    var configOption = 0;
                    Int32.TryParse((string)RouteData.Values["id"], out configOption);       // 0 default value if conversion fails
                   
                    var cid = Session["ClientId"];
                    int clientid = Convert.ToInt32(cid.ToString());
                    ViewBag.UserName = _adminManager.GetUserName(user.UserId, clientid);
                    string path = Request.RawUrl.ToString();

                    string CONFIG_PATH = Path.Combine(this.DataConfigPath, clientid + "\\" + portalId + "\\Configuration");
                    string LANG_PATH = Path.Combine(this.DataConfigPath, clientid + "\\" + portalId + "\\Languages");
                    string CSS_PATH = Path.Combine(this.DataConfigPath, clientid + "\\" + portalId + "\\Styles");
                    string JS_PATH = Path.Combine(this.DataConfigPath, clientid + "\\" + portalId + "\\Scripts");

                    var portalName = _adminManager.GetPortalName(portalId, clientid);
                    var langId = _adminManager.GetPortalLangId(clientid,portalId);

                    var lang= CreateLangDictionary();
                    var langFile = lang[langId]+".xml";
                    var css = CreateCSSDictionary();
                   
                    ConfigModel cnfgmodel = new Models.ConfigModel();
                    BreadcrumbViewModel BreadcrumbViewModel = new BreadcrumbViewModel();
                    BreadcrumbViewModel.NavigationList = new List<SelectListItem>();
                    BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "Portal Administration", Value = "Admin", Selected = false });
                    if (configOption != 0)
                    { 
                        BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = portalName, Value = "Admin/Edit/" + clientid + "/" + portalId, Selected = false }); 
                    }
                    switch (configOption)
                    {
                        case 1:
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "Configuration", Value = "Admin", Selected = true });
                            cnfgmodel.XmlContent = _adminManager.ReadXMLFile(CONFIG_PATH + "\\portal-config.xml");
                            break;
                        case 2:
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "Language", Value = "Admin", Selected = true });
                            cnfgmodel.XmlContent = _adminManager.ReadXMLFile(LANG_PATH + "\\" + langFile);
                            ViewBag.lang = langFile;
                            break;
                        case 3:
                            int csId = Convert.ToInt32(Request.QueryString["cssId"]);                            
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "CSS", Value = "Admin", Selected = true });
                            if(csId !=0)
                            {
                                var cssFile = css[csId];
                                cnfgmodel.TextContent = _adminManager.ReadTextFile(CSS_PATH + "\\" + cssFile);
                            }
                            break;
                        case 4:
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "Images", Value = "Admin", Selected = true });
                            break;
                        case 5:
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = "Script", Value = "Admin", Selected = true });
                            cnfgmodel.TextContent = _adminManager.ReadTextFile(JS_PATH+ "\\portal.js");
                            break;
                        default:
                            BreadcrumbViewModel.NavigationList.Add(new SelectListItem() { Text = portalName, Value = "Admin/Edit/" + clientid + "/" + portalId, Selected = true }); 
                            break;
                    }

                    cnfgmodel.PortalId = portalId;
                    cnfgmodel.LangId = langId;
                    cnfgmodel.RequestPath = path;
                    cnfgmodel.ConfigType = configOption;
                    cnfgmodel.BreadcrumbViewModel = BreadcrumbViewModel;
                    cnfgmodel.CssFiles = css;                    
                    cnfgmodel.ModifiedFilesModel = _adminManager.ReadConfigDetail(clientid, portalId, configOption);
                    return View(cnfgmodel);
                }
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.AdminBreadcrumbEditError,
                    new KBExceptionData("message", message), new KBExceptionData("msgType", msgType), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1));
                throw kbCustExp;
            }
        }

        [HttpPost]
         [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ConfigModel viewmodel)
        {
            int clientId = 0;
            try
            {
                var cid = Session["ClientId"];
                clientId = Convert.ToInt32(cid.ToString());         

                return View();
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.AdminBreadcrumbEditError,
                    new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
         [ValidateAntiForgeryToken]
        public async Task<ActionResult> Configuration(ConfigModel viewmodel, IEnumerable<HttpPostedFileBase> files)
        {
            int userId = 0;
            int clientId = 0;
            AdminUser user = null;
            int typeId = 0;
            int langId = 0;
            try
            {
                var cid = Session["ClientId"];
                clientId = Convert.ToInt32(cid.ToString());
                user = (PortalAPI.Models.AdminUser)Session["AdminUserSession"];
                userId = user.UserId;
                string xml = viewmodel.XmlContent;
                string scriptText = viewmodel.TextContent;
                typeId = viewmodel.ConfigType;
                langId = viewmodel.LangId;

                string CONFIG_PATH = Path.Combine(this.DataConfigPath, clientId + "\\" + viewmodel.PortalId + "\\Configuration");
                string LANG_PATH = Path.Combine(this.DataConfigPath, clientId + "\\" + viewmodel.PortalId + "\\Languages");
                string CSS_PATH = Path.Combine(this.DataConfigPath, clientId + "\\" + viewmodel.PortalId + "\\Styles");
                string JS_PATH = Path.Combine(this.DataConfigPath, clientId + "\\" + viewmodel.PortalId + "\\Scripts");

                string CLIENTPATH = clientId + "/" + viewmodel.PortalId;
                string EDITCONTROL = "Edit/" + CLIENTPATH;
                string[] imgExtn = { "jpg", "png", "gif" };

               switch(typeId)
               {
                   case 1:
                       XmlDocument newXml = new XmlDocument();
                       try { 
                           newXml.LoadXml(string.Format("{0}", xml));
                           var edited = _adminManager.WriteXMLFile(CONFIG_PATH + "\\portal-config.xml", newXml);
                           var config = _adminManager.AddConfigDetailPortal(viewmodel.PortalId, clientId, userId, 1, "portal-config.xml");
                           }
                       catch(Exception ex)
                       {
                           KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                               new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                               new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                           return RedirectToAction(EDITCONTROL + "/1", "Admin", new { Msgtype = "2" });
                       }
                       return RedirectToAction(EDITCONTROL + "/1", "Admin", new {  Msgtype = "1" });
                       

                   case 2: XmlDocument langXml = new XmlDocument();
                        var lang = CreateLangDictionary();
                        var langFile = lang[viewmodel.LangId] + ".xml";
                       try
                       {                          
                           langXml.LoadXml(string.Format("{0}", xml));
                           var edited = _adminManager.WriteXMLFile(LANG_PATH + "\\" + langFile, langXml);
                           var config = _adminManager.AddConfigDetailPortal(viewmodel.PortalId, clientId, userId, 2, langFile);
                       }
                       catch (Exception ex)
                       {
                           KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                               new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                               new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                           return RedirectToAction(EDITCONTROL + "/2", "Admin", new { Msgtype = "2" });
                       }
                       return RedirectToAction(EDITCONTROL + "/2", "Admin", new { lang = langFile, Msgtype = "1" });


                   case 3: var newcss = CreateCSSDictionary();
                       int csId = Convert.ToInt32(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["cssId"]);
                       
                       var cssFile = newcss[csId];
                       try
                       {
                           var edited = _adminManager.WriteTextFile(CSS_PATH + "\\" + cssFile, Convert.ToString(scriptText));
                           var config = _adminManager.AddConfigDetailPortal(viewmodel.PortalId, clientId, userId, 3, cssFile);
                       }
                       catch (Exception ex)
                       {
                           KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                               new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                               new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                           return RedirectToAction(EDITCONTROL + "/3", "Admin");
                       }
                       return RedirectToAction(EDITCONTROL + "/3", "Admin", new { Msgtype = "1" });

                   case 4:
                       try
                       {
                           var comPath = Path.Combine("knowledgebase\\PortalConfiguration", clientId + "\\" + viewmodel.PortalId + "\\" + "Images");
                           var folder = Path.Combine(_adminManager.DataPath, comPath);
                           if (!Directory.Exists(folder))
                               Directory.CreateDirectory(folder);

                           foreach (var file in files)
                           {
                               if (file.ContentLength > 0)
                               {
                                   var fileName = Path.GetFileName(file.FileName);                                  
                                   var path = Path.Combine(folder, fileName);
                                   var extn = fileName.Substring(fileName.Length - 3).ToLower();
                                   if (imgExtn.Any(s => extn.Contains(s)))
                                   {
                                       file.SaveAs(path);
                                       var confg = _adminManager.AddConfigDetailPortal(viewmodel.PortalId, clientId, userId, 4, fileName);
                                   }
                               }
                           }
                       }
                       catch (Exception ex)
                       {
                           KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                               new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                               new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                           return RedirectToAction(EDITCONTROL + "/4", "Admin");
                       }
                       return RedirectToAction(EDITCONTROL + "/4", "Admin");

                   case 5: 
                       try
                       {
                           var edited = _adminManager.WriteTextFile(JS_PATH + "\\portal.js", Convert.ToString(scriptText));
                           var config = _adminManager.AddConfigDetailPortal(viewmodel.PortalId, clientId, userId, 5, "portal.js");
                       }
                       catch (Exception ex)
                       {
                           KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                               new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                               new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                           return RedirectToAction(EDITCONTROL + "/5", "Admin");
                       }
                       return RedirectToAction(EDITCONTROL + "/5", "Admin", new { Msgtype = "1" });

                   default:break;
                }


               return RedirectToAction(EDITCONTROL, "Admin");
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigDetailPortal,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("userId", userId), new KBExceptionData("user.UserId", (user != null) ? user.UserId : -1),
                    new KBExceptionData("typeId", typeId), new KBExceptionData("langId", langId));
                throw kbCustExp;
            }
        }
         ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

         public ActionResult Logout()
        {
            Session["AdminUserSession"] = null;
            return RedirectToAction("Index", "Admin");
        }
         ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
       public Dictionary<int,string> CreateLangDictionary()
       {
           try
           {
               Dictionary<int, string> lang = new Dictionary<int, string>();
               lang[1] = "en-US"; lang[2] = "es-ES"; lang[3] = "fr-FR"; lang[5] = "ja-JP"; lang[6] = "de-DE"; lang[7] = "zh-CHT";
               lang[8] = "it-IT"; lang[9] = "nl-BE"; lang[10] = "sv-SE"; lang[11] = "ko-KR"; lang[14] = "zh-CHS"; lang[15] = "pt-PT";
               lang[16] = "pl-PL"; lang[17] = "el-GR"; lang[18] = "ru-RU"; lang[19] = "tr-TR"; lang[20] = "da-DK";
               return lang;
           }           
           catch (Exception ex)
           {
               KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminAddConfigLanguage);
               throw kbCustExp;
           }
       }
       public Dictionary<int, string> CreateCSSDictionary()
       {
           try
           {
               Dictionary<int, string> css = new Dictionary<int, string>();
               css[1] = "article.css"; css[2] = "browse.css"; css[3] = "foundation.min.css";
               css[4] = "home.css"; css[5] = "portal.css"; css[6] = "search.css";
               css[7] = "solutionfinder.css";
               css[8] = "normalize.css"; css[9] = "nav.css"; 
               return css;
           }
           catch (Exception ex)
           {
               KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminConfigError);
               throw kbCustExp;
           }
       }
      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
       public ActionResult Delete()
       {
           try
           {
                int portalId = Convert.ToInt32(Request.QueryString["pid"]);
                string img = Request.QueryString["filename"];
                var cid = Session["ClientId"];
                int clientid = Convert.ToInt32(cid.ToString());
                _adminManager.DeleteImageFile(clientid, portalId, img);
                return RedirectToAction("Edit/" + clientid + "/" + portalId+"/4", "Admin");
           }
           catch (Exception ex)
           {
               KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminConfigError);
               throw kbCustExp;
           }
       }
       public ActionResult Download()
       {
           try
           {
               int portalId = Convert.ToInt32(Request.QueryString["pid"]);
               string img = Request.QueryString["filename"];
               var cid = Session["ClientId"];
               int clientid = Convert.ToInt32(cid.ToString());
               var folder = Path.Combine(_adminManager.DataPath, "knowledgebase\\PortalConfiguration"+ "\\"+ clientid + "\\" + portalId + "\\" + "Images");
               return File(folder + "\\" + img, "image", img);             
           }
           catch (Exception ex)
           {
               KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AdminConfig, KBErrorHandler.GetMethodName(), GeneralResources.AdminConfigError);
               throw kbCustExp;
           }
       }
    }
}