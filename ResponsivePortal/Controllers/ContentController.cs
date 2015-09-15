using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NLog;
using PortalAPI.Models;
using PortalAPI.Managers.Concrete;
using System.IO;
using PortalAPI.Repositories.Concrete;
using KBCommon.KBException;
using ResponsivePortal.Resources;
using ResponsivePortal.Filters;
namespace ResponsivePortal.Controllers
{
    [CustomErrorHandler]
    public class ContentController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private AdminManager _adminManager;
        private int clientId;
        private int portalId;
        private string fileName;
        private int kbId;
        private int articleId;

        public ContentController(AdminManager adminManager)
        {
            this._adminManager = adminManager;
        }

        //[OutputCache(Location = System.Web.UI.OutputCacheLocation.Client, Duration = 86400, VaryByParam = "rand")]       
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Scripts()
        {
            return GetData(FileType.js);
        }
        public ActionResult Styles()
        {
            return GetData(FileType.css);
        }
        public ActionResult Images()
        {
            return GetData(FileType.image);
        }

        public ActionResult ArticleImage()
        {
            return GetArticleResources("_images");
        }
        public ActionResult ArticleFiles()
        {
            return GetArticleResources("_files");
        }

        private FileContentResult GetData(FileType fileType)
        {
            FileContentResult fr = null;
            ReadDataFromRouteData();
            string folderPrefix = string.Empty;
            switch (fileType.ToString())
            {
                case "js":
                    folderPrefix = "Scripts";
                    break;
                case "css":
                    folderPrefix = "Styles";
                    break;
                case "image":
                    folderPrefix = "Images";
                    break;
            }
            try
            {
                string fileFullPath = Path.Combine(_adminManager.DataPath, "knowledgebase", "portalConfiguration", clientId.ToString(), portalId.ToString(), folderPrefix, fileName);
                System.IO.MemoryStream s = _adminManager.ReadFileStream(fileFullPath);
                byte[] bts = new byte[s.Length];
                s.Read(bts, 0, bts.Length);
                string contentType = Mime.FromExtension(Path.GetExtension(fileFullPath));
                fr = new FileContentResult(bts, contentType);
            }
            catch (IOException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.IOError,  
                    new KBExceptionData("fileType", fileType.ToString()), new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId),
                    new KBExceptionData("folderPrefix", folderPrefix), new KBExceptionData("fileName", fileName));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError,
                    new KBExceptionData("fileType", fileType.ToString()), new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId),
                    new KBExceptionData("folderPrefix", folderPrefix), new KBExceptionData("fileName", fileName));
                throw kbCustExp;
            }
            return fr;
        }

        private FileContentResult GetArticleResources(string type)
        {
            FileContentResult fr = null;
            ReadDataFromRouteDataForImage();
            try
            {
                string fileFullPath = Path.Combine(_adminManager.DataPath, "knowledgebase", "articlesPublished", clientId.ToString(), kbId.ToString(), articleId.ToString()+ type, fileName);
                System.IO.MemoryStream s = _adminManager.ReadFileStream(fileFullPath);
                byte[] bts = new byte[s.Length];
                s.Read(bts, 0, bts.Length);
                string contentType = Mime.FromExtension(Path.GetExtension(fileFullPath));
                fr = new FileContentResult(bts, contentType);
            }
            catch (IOException ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.IOError,
                    new KBExceptionData("type", type), new KBExceptionData("kbId", kbId.ToString()), new KBExceptionData("articleId", articleId), new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId),
                    new KBExceptionData("fileName", fileName));
                throw kbCustExp;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.LoadContent, KBErrorHandler.GetMethodName(), GeneralResources.GeneralError,
                    new KBExceptionData("type", type), new KBExceptionData("kbId", kbId.ToString()), new KBExceptionData("articleId", articleId), new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId),
                    new KBExceptionData("fileName", fileName));
                throw kbCustExp;
            }
            return fr;
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

            if (RouteData.Values["fileName"].GetType() == typeof(System.String))
            {
                fileName = (string)RouteData.Values["fileName"];
            }
            else
            {
                fileName = (string)RouteData.Values["fileName"];
            }
        }

        private void ReadDataFromRouteDataForImage()
        {
            if (RouteData.Values["clientId"].GetType() == typeof(System.Int32))
            {
                clientId = (int)RouteData.Values["clientId"];
            }
            else
            {
                clientId = int.Parse((string)RouteData.Values["clientId"]);
            }

            if (RouteData.Values["articleId"].GetType() == typeof(System.Int32))
            {
                articleId = (int)RouteData.Values["articleId"];
            }
            else
            {
                articleId = int.Parse((string)RouteData.Values["articleId"]);
            }

            if (RouteData.Values["kbId"].GetType() == typeof(System.Int32))
            {
                kbId = (int)RouteData.Values["kbId"];
            }
            else
            {
                kbId = int.Parse((string)RouteData.Values["kbId"]);
            }
            if (RouteData.Values["fileName"].GetType() == typeof(System.String))
            {
                fileName = (string)RouteData.Values["fileName"];
            }
            else
            {
                fileName = (string)RouteData.Values["fileName"];
            }
        }
    }
}