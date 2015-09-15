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
using System.Web.Routing;
using ResponsivePortal.Filters;
using KBCommon.KBException;
using ResponsivePortal.Resources;

namespace ResponsivePortal.Controllers
{
   
    [CustomErrorHandler]
    public class AccountController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private UsersManager _usersManager;
        private TokenManager _tokenManager;
        private ActiveDirectoryManager _activeDirectoryManager;
        private int portalId;
        private int clientId;
        private Portal _portal;
        public Dictionary<string, string> resources = new Dictionary<string, string>();
        public AccountController(UsersManager usersManager, TokenManager tokenManager)
        {
            this._usersManager = usersManager;
            this._tokenManager = tokenManager;
        }
        
        public AccountController(UsersManager usersManager, TokenManager tokenManager, ActiveDirectoryManager activeDirectoryManager)
        {
            this._usersManager = usersManager;
            this._tokenManager = tokenManager;
            this._activeDirectoryManager = activeDirectoryManager;
        }

        public AccountController(UsersManager usersManager)
        {
            this._usersManager = usersManager;
        }

        public AccountController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }

        //
        // GET: /Account/Login        
        public ActionResult Login(string returnUrl, string Message)
        {
            try
            {
                ReadDataFromRouteData("login");
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Title = "log in";
                ViewBag.Message = Message;

                int portalId;
                if (RouteData.Values["portalId"].GetType() == typeof(System.Int32))
                    portalId = (int)RouteData.Values["portalId"];
                else portalId = int.Parse((string)RouteData.Values["portalId"]);
                if (RouteData.Values["clientId"].GetType() == typeof(System.Int32))
                    clientId = (int)RouteData.Values["clientId"];
                else clientId = int.Parse((string)RouteData.Values["clientId"]);
                
                var user = (PortalAPI.Models.User)Session["UserSession_" + portalId.ToString()];
                if (user != null)
                {
                    var routrDict = new RouteValueDictionary();
                    routrDict.Add("Controller", "Home");
                    routrDict.Add("Action", "Index");
                    routrDict.Add("clientId", clientId);
                    routrDict.Add("portalId", portalId);

                    return RedirectToLocal(returnUrl, routrDict);
                }
                ViewBag.clientId = clientId; //needed for resetpassword
                ViewBag.portalId = portalId; //needed for resetpassword
                LoginViewModel viewModel = new LoginViewModel();
                viewModel.Resources = resources;
                viewModel.clientId = clientId;
                viewModel.portalId = portalId;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoginAction, KBErrorHandler.GetMethodName(), GeneralResources.LoginError,
                    new KBExceptionData("returnUrl", returnUrl), new KBExceptionData("Message", Message));
                throw kbCustExp;
            }
        }

        public ActionResult ADLogin(string username,string returnUrl, string Message)
        {
            try
            {
                ReadDataFromRouteData("login");
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Title = "log in";
                ViewBag.Message = Message;

                int portalId;
                if (RouteData.Values["portalId"].GetType() == typeof(System.Int32))
                    portalId = (int)RouteData.Values["portalId"];
                else portalId = int.Parse((string)RouteData.Values["portalId"]);
                if (RouteData.Values["clientId"].GetType() == typeof(System.Int32))
                    clientId = (int)RouteData.Values["clientId"];
                else clientId = int.Parse((string)RouteData.Values["clientId"]);
                var user = (PortalAPI.Models.User)Session["UserSession_" + portalId.ToString()];
                if (user != null)
                {
                    var routrDict = new RouteValueDictionary();
                    routrDict.Add("Controller", "Home");
                    routrDict.Add("Action", "Index");
                    routrDict.Add("clientId", clientId);
                    routrDict.Add("portalId", portalId);
                    return RedirectToLocal(returnUrl, routrDict);
                }
                else
                {
                    if(_activeDirectoryManager.FindUserInAD(username))
                    {
                        PortalAPI.Models.User ADuser = _usersManager.ADLogin(username);
                        if (ADuser != null)
                        {
                            SessionExtensions.LogPortalVisit(ADuser.UserId, portalId, clientId, 2, "");
                            ADuser.isActiveDirectoryUser = true;
                            var appUser = new ApplicationUser() { UserName = ADuser.LoginName };
                            SignInAsync(appUser, false);
                            Session.Add("UserSession_" + portalId.ToString(), ADuser);

                            var routrDict = new RouteValueDictionary();
                            routrDict.Add("Controller", "Home");
                            routrDict.Add("Action", "Index");
                            routrDict.Add("clientId", clientId);
                            routrDict.Add("portalId", portalId);
                            return RedirectToLocal(returnUrl, routrDict);
                            //return RedirectToAction("Index/" + clientId + "/" + portalId, "Home", new { Message = "1" });
                        }
                        else
                        {
                            ViewBag.Message = "1";
                            ViewBag.clientId = clientId;
                            ViewBag.portalId = portalId;
                            return RedirectToAction("Login/" + clientId + "/" + portalId, "Account", new { Message = "0" });
                        }
                    }
                    else
                    {
                        ViewBag.Message = "1";
                        ViewBag.clientId = clientId;
                        ViewBag.portalId = portalId;
                        return RedirectToAction("Login/" + clientId + "/" + portalId, "Account", new { Message = "1" });
                    }
                }
                //ViewBag.clientId = clientId; //needed for resetpassword
                //ViewBag.portalId = portalId; //needed for resetpassword
                //LoginViewModel viewModel = new LoginViewModel();
                //viewModel.Resources = resources;
                //viewModel.clientId = clientId;
                //viewModel.portalId = portalId;

                //return View(viewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoginAction, KBErrorHandler.GetMethodName(), GeneralResources.LoginError,
                    new KBExceptionData("returnUrl", returnUrl), new KBExceptionData("Message", Message));
                throw kbCustExp;
            }
        }
        // GET: /Account/Error      
        public ActionResult Error()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AccountActivity, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError);
                throw kbCustExp;
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl, int portalId)
        {
            try
            {
                ReadDataFromRouteData("login");
                model.Resources = resources;
                model.portalId = portalId;
                model.clientId = clientId;

                if (ModelState.IsValid)
                {
                    // Form data will be validated against the information stored in Database
                    //and user object will be returned if its authenticated.

                    PortalAPI.Models.User user = null;
                    var ADSIEnabled = false;
                    PortalAPI.Repositories.Concrete.DataConfigurationRepository dataConfigurationRepository = new PortalAPI.Repositories.Concrete.DataConfigurationRepository(System.Web.HttpContext.Current.Application["KBDataPath"].ToString(), System.Web.HttpContext.Current.Application["KBInstallPath"].ToString());

                    if (dataConfigurationRepository != null)
                    { ADSIEnabled = dataConfigurationRepository.ADEnabled; }

                    if (ADSIEnabled)
                    {
                        var isvalidADUser = _activeDirectoryManager.Authenticate(model.UserName, model.Password);
                        if (isvalidADUser)
                        {
                            user = _usersManager.ADLogin(model.UserName);
                        }
                        else
                        {
                            user = _usersManager.Login(model.UserName, model.Password);
                        }
                    }
                    else
                    {
                        user = _usersManager.Login(model.UserName, model.Password);
                    }
                    if (user != null)
                    {
                        SessionExtensions.LogPortalVisit(user.UserId, portalId, clientId, 2, "");

                        var appUser = new ApplicationUser() { UserName = user.LoginName };
                        await SignInAsync(appUser, model.RememberMe);
                        Session.Add("UserSession_" + portalId.ToString(), user);
                        var routrDict = new RouteValueDictionary();
                        routrDict.Add("Controller", "Home");
                        routrDict.Add("Action", "Index");
                        routrDict.Add("clientId", clientId);
                        routrDict.Add("portalId", portalId);
                        return RedirectToLocal(returnUrl, routrDict);
                    }
                    else
                    {
                        //ModelState.AddModelError("", "Invalid username or password.");
                        ViewBag.Message = "1";
                        ViewBag.clientId = clientId;
                        ViewBag.portalId = portalId;
                        return RedirectToAction("Login/" + model.clientId + "/" + portalId, "Account", new { Message = "1" });
                    }

                }

                return RedirectToAction("Login/" + model.clientId + "/" + portalId, "Account");
                //return View(model);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoginAction, KBErrorHandler.GetMethodName(), GeneralResources.LoginError,
                    new KBExceptionData("returnUrl", returnUrl), new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }


        // GET: /Account/Reset Password       
        public ActionResult ResetPassword(string Message)
        {
            try
            {
                ViewBag.Message = Message;
                int portalId;
                if (RouteData.Values["portalId"].GetType() == typeof(System.Int32))
                    portalId = (int)RouteData.Values["portalId"];
                else portalId = int.Parse((string)RouteData.Values["portalId"]);
                if (RouteData.Values["clientId"].GetType() == typeof(System.Int32))
                    clientId = (int)RouteData.Values["clientId"];
                else clientId = int.Parse((string)RouteData.Values["clientId"]);
              
                ViewBag.clientId = clientId;
                ViewBag.portalId = portalId;  
                ReadDataFromRouteData("resetpassword");
                ResetPasswordViewModel viewModel = new ResetPasswordViewModel();
                viewModel.Resources = resources;
                viewModel.clientId = clientId;
                viewModel.portalId = portalId;                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ResetPassword, KBErrorHandler.GetMethodName(), GeneralResources.ResetPasswordError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }

        // GET: /Account/ Submit Reset Password
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                ReadDataFromRouteData("resetpassword");
                model.Resources = resources;
                model.portalId = portalId;
                model.clientId = clientId;
                PortalAPI.Models.User user = _usersManager.GetUserByName(model.UserName);
                ViewBag.clientId = clientId;
                ViewBag.portalId = portalId; 
                if (user == null)
                {
                    ViewBag.Message = "1";         
                    return RedirectToAction("ResetPassword/" + clientId + "/" + portalId, "Account", new { Message = "1" });
                }
                // Add logic to send email to user and redirect to home page.

                bool result = _tokenManager.RequestForNewPassword(model.UserName, UserType.ExternalUser, _portal.Language.Id,portalId,clientId);
                return RedirectToAction("Login/" + model.clientId + "/" + portalId, "Account", new { Message = "2" });
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ResetPassword, KBErrorHandler.GetMethodName(), GeneralResources.ResetPasswordError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }

     
        // GET: /Account/ Cancel Reset Password       
        public ActionResult ChangePassword(string Message)
        {
            try
            {
                ReadDataFromRouteData("changePassword");
                ChangePasswordViewModel viewModel = new ChangePasswordViewModel();
                viewModel.TokenString = Request.QueryString["resetToken"];
                viewModel.client_Id = clientId;
                viewModel.portal_Id = portalId;
                viewModel.Resources = resources;
                ViewBag.Message = Message;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ChangePassword, KBErrorHandler.GetMethodName(), GeneralResources.ChangePasswordError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel viewmodel)
        {
            try
            {               
                string name = viewmodel.TokenString;
                if (name == "") { name = Request.QueryString["resetToken"]; }
                int cId = viewmodel.client_Id;
                int pId = viewmodel.portal_Id;
                ReadDataFromRouteData("changePassword");
                viewmodel.Resources = resources;
                
                if((viewmodel.NewPassword==null)||(viewmodel.ConfirmPassword==null))
                {
                    return RedirectToAction("ChangePassword/" + clientId + "/" + portalId, "Account", new { Message = "3", resetToken = name });
                }
                if (viewmodel.NewPassword != viewmodel.ConfirmPassword)
                {
                    return RedirectToAction("ChangePassword/" + clientId + "/" + portalId, "Account", new { Message = "1", resetToken = name });
                }
                if ((viewmodel.NewPassword.Length < 6) || (viewmodel.ConfirmPassword.Length < 6))
                {
                    return RedirectToAction("ChangePassword/" + clientId + "/" + portalId, "Account", new { Message = "2", resetToken = name });
                }
                bool result = _tokenManager.SavePassword(name, viewmodel.NewPassword, clientId, portalId);
                if(result==true)
                    return RedirectToAction("Login/" + clientId + "/" + portalId, "Account", new { Message = "3" });
                else
                    return RedirectToAction("ChangePassword/" + clientId + "/" + portalId, "Account", new { Message = "4", resetToken = name });
               
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.ChangePassword, KBErrorHandler.GetMethodName(), GeneralResources.ChangePasswordError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }

    
        //
        // POST: /Account/LogOff
        public ActionResult LogOff(int clientId, int portalId)
        {           
            try
            {
                Session.UpdateLogoff(portalId, clientId);
                Session["UserSession_" + portalId.ToString()] = null;
                AuthenticationManager.SignOut();
                var routrDict = new RouteValueDictionary();
                routrDict.Add("Controller", "Account");
                routrDict.Add("Action", "Login");
                routrDict.Add("clientId", clientId);
                routrDict.Add("portalId", portalId);
                //return RedirectToAction("Index", "Home", routrDict);
                return RedirectToAction("Login/" + clientId + "/" + portalId, "Account", new { Message = "0" });
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LogoutAction, KBErrorHandler.GetMethodName(), GeneralResources.LogOutError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }

        public ActionResult LogOffNotAuthorized(int clientId, int portalId)
        {
            try
            {
                Session["UserSession_" + portalId.ToString()] = null;
                AuthenticationManager.SignOut();
                return View("Error");
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LogoutAction, KBErrorHandler.GetMethodName(), GeneralResources.LogOutError,
                    new KBExceptionData("portalId", portalId), new KBExceptionData("clientId", clientId));
                throw kbCustExp;
            }
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && UserManager != null)
                {
                    UserManager.Dispose();
                    UserManager = null;
                }
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.AccountActivity, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError);
                throw kbCustExp;
            }
        }
        private void ReadDataFromRouteData(string resourceName)
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
            string lang = "English";
            if (_portal != null) { lang = _portal.Language.Name; }
            //resource object
            resources = Session.Resource(portalId, clientId, resourceName, lang);

        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";


        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            // Customzing OWIN cookie authentication to autheticate a portal user.
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            var id = new ClaimsIdentity(claims,
                                        DefaultAuthenticationTypes.ApplicationCookie);
            var ctx = Request.GetOwinContext();
            var authenticationManager = ctx.Authentication;
            authenticationManager.SignIn(id);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl, RouteValueDictionary route)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home", route);
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}
