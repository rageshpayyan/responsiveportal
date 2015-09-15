using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using PortalAPI.Models;
using ResponsivePortal.Models;
using System.Web.Mvc;
using PortalAPI.Repositories.Concrete;
using NetMime = System.Net.Mime.MediaTypeNames;

using System.Collections;
using KBCommon.KBException;
using ResponsivePortal.Resources;
using NLog;
using System.Text.RegularExpressions;

namespace ResponsivePortal
{
    public static class Utilities
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static string GetImageUrl(int clientId, int portalId, string imageUrl)
        {
            if (imageUrl != string.Empty) return "/content/Images/" + clientId.ToString() + "/" + portalId.ToString() + "/" + imageUrl.ToString();
            return imageUrl;
        }

        public static CommonViewModel CreateCommonViewModel(int clientId, int portalId, PortalType portalType, PortalUIConfiguration configuration, string selectedModule)
        {
            CommonViewModel commonViewModel = new CommonViewModel();
            commonViewModel.HeaderViewModel = CreateHeaderViewModel(clientId, portalId, portalType, configuration, selectedModule);
            commonViewModel.FooterViewModel = CreateFooterViewModel(configuration);
            return commonViewModel;
        }
        private static HeaderViewModel CreateHeaderViewModel(int clientId, int portalId, PortalType portalType, PortalUIConfiguration configuration, string selectedModule)
        {
            try
            {
                HeaderViewModel headerViewModel = new HeaderViewModel();
                headerViewModel.NavigationList = new List<SelectListItem>();
                headerViewModel.BeforePortal = configuration.BeforePortal;
                headerViewModel.portalId = portalId;
                headerViewModel.clientId = clientId;
                headerViewModel.portalType = portalType;

                List<PortalModule> OrderedModuleList = configuration.Modules.OrderBy(x => x.DisplayOrder).ToList();
                List<PortalModule> SelectedModuleList = OrderedModuleList.Where(x => x.Display == true).ToList();
                if (configuration.ShowPortalNavigation)
                {
                    foreach (PortalModule module in SelectedModuleList)
                    {
                        SelectListItem navItem = new SelectListItem() { Text = module.ModuleName, Value = module.ModuleType, Selected = false };
                        if (module.ModuleType == selectedModule)
                        {
                            navItem.Selected = true;
                        }
                        if (module.ModuleType == "articles")
                        {
                            navItem.Value = "article";
                        }
                        headerViewModel.NavigationList.Add(navItem);
                    }
                }
                if (HttpContext.Current.Session["UserSession_" + portalId.ToString()] != null)
                {
                    var usr = (User)HttpContext.Current.Session["UserSession_" + portalId.ToString()];
                    if (usr != null)
                    {
                        headerViewModel.isActiveDiretoryUser = usr.isActiveDirectoryUser;
                    }

                }
                return headerViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("portalType", portalType), new KBExceptionData("selectedModule", selectedModule), new KBExceptionData("configuration.AfterPortal", configuration.AfterPortal));
                throw kbCustExp;
            }
        }

        public static ArticlePartialViewModel CreateArticlePartialViewModel(ArticleItem articleItem, ArticleModule articleModule,
            ArrayList categoryNamesList, ArrayList attributeNamesList, PortalType portalType,
            Dictionary<string, string> resources, int clientId, int portalId, string adminURL)
        {
            ArticlePartialViewModel articlePartialViewModel = new ArticlePartialViewModel();
            DataConfigurationRepository dataConfigurationRepository = new
                            DataConfigurationRepository(HttpContext.Current.Application["KBDataPath"].ToString(), HttpContext.Current.Application["KBInstallPath"].ToString());
            articlePartialViewModel.Resources = resources;
            articlePartialViewModel.ArticleItem = articleItem;
            articlePartialViewModel.clientId = clientId;
            articlePartialViewModel.portalId = portalId;
            if (articleModule != null)
            {
                articlePartialViewModel.ArticleConfiguration = articleModule;
            }
            else
            {
                //---- set defaults ------

            }
            List<BreadcrumbViewModel> categories = new List<BreadcrumbViewModel>();
            foreach (List<string> names in categoryNamesList)
            {
                BreadcrumbViewModel itemList = new BreadcrumbViewModel();
                itemList.NavigationList = new List<SelectListItem>();
                foreach (string name in names)
                {
                    itemList.NavigationList.Add(new SelectListItem() { Text = name, Value = string.Empty, Selected = false });
                }
                categories.Add(itemList);
            }
            articlePartialViewModel.Categories = categories;
            articlePartialViewModel.Attributes = new List<BreadcrumbViewModel>();
            List<BreadcrumbViewModel> attr = new List<BreadcrumbViewModel>();
            foreach (List<string> names in attributeNamesList)
            {
                BreadcrumbViewModel itemList = new BreadcrumbViewModel();
                itemList.NavigationList = new List<SelectListItem>();
                foreach (string name in names)
                {
                    itemList.NavigationList.Add(new SelectListItem() { Text = name, Value = string.Empty, Selected = false });
                }
                attr.Add(itemList);
            }
            articlePartialViewModel.Attributes = attr;
            articlePartialViewModel.ImageLinks = new List<ImageLinkViewModel>();
            ImageLinkViewModel imageLinkViewModel;

            //check Favorite is enabled
            if (articlePartialViewModel.ArticleConfiguration.articleControlsProperties.ArticleFavoriteDisplay)
            {
                imageLinkViewModel = new ImageLinkViewModel() { Icon = (articlePartialViewModel.ArticleItem.Favorite == true &&
                    (portalType != PortalType.Open && portalType != PortalType.Registration)) ? 
                    @Utilities.GetImageUrl(clientId, portalId, "favoriteactive.png") : @Utilities.GetImageUrl(clientId, portalId, "favorite.png"),
                                                                Link = "/Article/FavoriteArticle/" + clientId + "/" + portalId,
                                                                Title = Utilities.GetResourceText(resources, "CONTROLS_FAVORITELABEL"),
                                                                ToggleIcon = (articlePartialViewModel.ArticleItem.Favorite == true ||
                                                                portalType == PortalType.Open || portalType == PortalType.Registration) ? 
                                                                @Utilities.GetImageUrl(clientId, portalId, "favorite.png") : 
                                                                @Utilities.GetImageUrl(clientId, portalId, "favoriteactive.png")
                };
                imageLinkViewModel.Id = "favorites";
                articlePartialViewModel.ImageLinks.Add(imageLinkViewModel);
            }

            //check Subscription is enabled
            if (articlePartialViewModel.ArticleConfiguration.articleControlsProperties.ArticleSubscribeDisplay)
            {
                imageLinkViewModel = new ImageLinkViewModel() { Icon = @Utilities.GetImageUrl(clientId, portalId, "subscribe.png"), 
                    Link = "", Title = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBELABEL"), 
                    ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "subscribe_selected.png") };
                // imageLinkViewModel = new ImageLinkViewModel() { Icon = (articlesViewModel.ArticlePartialViewModel.ArticleItem.SubscriptionStatus == true && (_portal.PortalType != PortalType.Open && _portal.PortalType != PortalType.Registration)) ? @Utilities.GetImageUrl(clientId, portalId, "subscribe_select.png") : @Utilities.GetImageUrl(clientId, portalId, "subscribe.png"), Link = "/Article/ToggleArticleSubscription/" + clientId + "/" + portalId, Title = Utilities.GetResourceText(resources, "CONTROLS_SUBSCRIBELABEL"), ToggleIcon = (articlesViewModel.ArticlePartialViewModel.ArticleItem.SubscriptionStatus == true || _portal.PortalType == PortalType.Open || _portal.PortalType == PortalType.Registration) ? @Utilities.GetImageUrl(clientId, portalId, "subscribe.png") : @Utilities.GetImageUrl(clientId, portalId, "subscribe.png") };
                //imageLinkViewModel = new ImageLinkViewModel() { Icon = @Utilities.GetImageUrl(clientId, portalId, "subscribe.png"), Link = "", Title = Utilities.GetResourceText(resources,"CONTROLS_SUBSCRIBELABEL"), ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "subscribe.png") };
                imageLinkViewModel.Id = "subscribe";
                articlePartialViewModel.ImageLinks.Add(imageLinkViewModel);
            }
            //Check share is enabled
            if (articlePartialViewModel.ArticleConfiguration.articleControlsProperties.ArticleShareDisplay)
            {
                imageLinkViewModel = new ImageLinkViewModel() { Icon = @Utilities.GetImageUrl(clientId, portalId, "share.png"), 
                    Link = "", Title = Utilities.GetResourceText(resources, "CONTROLS_SHARELABEL"), 
                    ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "share_selected.png") };
                imageLinkViewModel.Id = "share";
                articlePartialViewModel.ImageLinks.Add(imageLinkViewModel);
            }
            if (articlePartialViewModel.ArticleConfiguration.articleControlsProperties.ArticleEditDisplay)
            {
                imageLinkViewModel = new ImageLinkViewModel() { Icon = @Utilities.GetImageUrl(clientId, portalId, "edit.png"),
                                                                Link = adminURL + "/index.aspx?aid=",
                                                                Title = Utilities.GetResourceText(resources, "CONTROLS_EDITLABEL"), 
                    ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "edit.png") };
                imageLinkViewModel.Id = "edit";
                articlePartialViewModel.ImageLinks.Add(imageLinkViewModel);
            }
            articlePartialViewModel.Attachments = articleItem.Attachments;
            return articlePartialViewModel;
        }

        public static ArticleShareViewModel GetShareViewModel(int articleId, string title, string shareUrl, string emailShareMessageBody,
            ArticleShareProperties articleShareProperties, Dictionary<string, string> resources, int clientId, int portalId)
        {
            try
            {
                ArticleShareViewModel shareViewModel = new ArticleShareViewModel();
                shareViewModel.ShareLinkList = new List<ImageLinkViewModel>();
                if (articleShareProperties.EmailDisplay)
                {
                    shareViewModel.EmailShareMessageBody = emailShareMessageBody;
                    shareViewModel.ShareLinkList.Add(new ImageLinkViewModel() { Icon = GetImageUrl(clientId, portalId, "share_mail.png"), Link = "", Title = Utilities.GetResourceText(resources, "CONTROLS_SHARE_EMAIL_TITLE"), ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "share_email.png"), Id = "share_email" });
                }
                if (articleShareProperties.FacebookDisplay)
                {
                    string facebookLink = PortalAPI.Utilities.FacebookURL + "/share.php?u=" + shareUrl;
                    shareViewModel.ShareLinkList.Add(new ImageLinkViewModel() { Icon = GetImageUrl(clientId, portalId, "share_facebook.png"), Link = facebookLink, Title = Utilities.GetResourceText(resources, "CONTROLS_SHARE_FACEBOOK_TITLE"), ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "social_facebook.png"), Id = "share_facebook" });
                }
                if (articleShareProperties.TwitterDisplay)
                {
                    string twitterLink = PortalAPI.Utilities.TwitterURL + "/intent/tweet?text=" + "\"" + title + "\".+" + shareUrl;
                    shareViewModel.ShareLinkList.Add(new ImageLinkViewModel() { Icon = GetImageUrl(clientId, portalId, "share_twitter.png"), Link = twitterLink, Title = Utilities.GetResourceText(resources, "CONTROLS_SHARE_TWITTER_TITLE"), ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "social_twitter.png"), Id = "share_twitter" });
                }
                if (articleShareProperties.RedditDisplay)
                {
                    string redditLink = PortalAPI.Utilities.RedditURL + "/submit?url=" + shareUrl + "&title=" + title;
                    shareViewModel.ShareLinkList.Add(new ImageLinkViewModel() { Icon = GetImageUrl(clientId, portalId, "share_reddit.png"), Link = redditLink, Title = Utilities.GetResourceText(resources, "CONTROLS_SHARE_REDDIT_TITLE"), ToggleIcon = @Utilities.GetImageUrl(clientId, portalId, "share.png"), Id = "share_reddit" });
                }

                shareViewModel.ArticleShareUrl = shareUrl;
                return shareViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.GetViewModel, KBErrorHandler.GetMethodName(), GeneralResources.GetViewModelError,
                    new KBExceptionData("clientId", clientId), new KBExceptionData("portalId", portalId), new KBExceptionData("articleId", articleId), new KBExceptionData("title", title),
                    new KBExceptionData("shareUrl", shareUrl), new KBExceptionData("emailShareMessageBody", emailShareMessageBody));
                throw kbCustExp;
            }
        }
        private static FooterViewModel CreateFooterViewModel(PortalUIConfiguration configuration)
        {
            try
            {
                FooterViewModel footerViewModel = new FooterViewModel();
                footerViewModel.Content = configuration.AfterPortal;
                return footerViewModel;
            }
            catch (Exception ex)
            {
                KBCustomException kbCustExp = KBCustomException.ProcessException(ex, KBOp.CreateViewModel, KBErrorHandler.GetMethodName(), GeneralResources.CreateViewModelError,
                    new KBExceptionData("configuration.BeforePortal", configuration.BeforePortal), new KBExceptionData("configuration.AfterPortal", configuration.AfterPortal));
                throw kbCustExp;
            }
        }

        public static string getModuleText(HeaderViewModel headerVM, string headerModuleValue)
        {
            string mText = headerVM.NavigationList.Find(r => r.Value == headerModuleValue) != null ? headerVM.NavigationList.Find(r => r.Value == headerModuleValue).Text : string.Empty;
            return mText;
        }

        public static string GetResourceText(Dictionary<string, string> Resources, string key)
        {
            if (Resources != null && Resources.ContainsKey(key)) return Resources[key];
            return key;
        }

        public static string GetResourceText(Dictionary<string, string> Resources, string key, string defaultText)
        {
            if (Resources != null && Resources.ContainsKey(key)) return Resources[key];
            return defaultText;
        }

        public static string GetFileType(string fileext)
        {
            string filetype = string.Empty;
            switch (fileext)
            {
                case "docx":
                case "doc":
                    filetype = "Word";
                    break;
                case "xls":
                case "xlsx":
                    filetype = "Excel";
                    break;
                case "htm":
                case "html":
                    filetype = "HTML";
                    break;
                case "pdf":
                    filetype = "PDF";
                    break;
                case "pptx":
                case "ppt":
                    filetype = "PPT";
                    break;
                default:
                    filetype = "";
                    break;
            }
            return filetype;
        }

        public static string GetResourceFileName(string language)
        {
            string fileName = "en-us.xml";
            switch (language.ToLower())
            {
                case "english":
                    fileName = "en-us.xml";
                    break;
                case "danish":
                    fileName = "da-DK.xml";
                    break;
                case "german":
                    fileName = "de-DE.xml";
                    break;
                case "greek":
                    fileName = "el-GR.xml";
                    break;
                case "spanish":
                    fileName = "es-ES.xml";
                    break;
                case "french":
                    fileName = "fr-FR.xml";
                    break;
                case "italian":
                    fileName = "it-IT.xml";
                    break;
                case "japanese":
                    fileName = "ja-JP.xml";
                    break;
                case "korean":
                    fileName = "ko-KR.xml";
                    break;
                case "dutch":
                    fileName = "nl-BE.xml";
                    break;
                case "polish":
                    fileName = "pl-PL.xml";
                    break;
                case "portuguese":
                    fileName = "pt-PT.xml";
                    break;
                case "russian":
                    fileName = "ru-RU.xml";
                    break;
                case "swedish":
                    fileName = "sv-SE.xml";
                    break;
                case "turkish":
                    fileName = "tr-TR.xml";
                    break;
                case "chinese (simplified)":
                    fileName = "zh-CHS.xml";
                    break;
                case "chinese (traditional)":
                    fileName = "zh-CHT.xml";
                    break;
            }
            return fileName;
        }

        public static string GetHistoryTextForSolutionFinder(string text)
        {
            if (text.ToLower().Contains("id") && text.ToLower().Contains("history"))
            {
                Regex rx = null;
                Match m = null;
                
                rx = new Regex(@"<p id=""history[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                m = rx.Match(text);

                if (m.Success)
                {
                    text = m.Groups[1].Value;
                }
                return text;
            }
            else return text;
        }

        public static string UpdateArticleURlInArticleContent(string content, string toReplace, string replaceWith)
        {
            DataConfigurationRepository dataConfigurationRepository = new
                             DataConfigurationRepository(HttpContext.Current.Application["KBDataPath"].ToString(), HttpContext.Current.Application["KBInstallPath"].ToString());
            var newContent = content.Replace(toReplace, replaceWith);
            newContent = newContent.Replace("|' target=_parent>", "' target=_parent>");
            return newContent.Replace(dataConfigurationRepository.AdminURL, dataConfigurationRepository.PS5URL);
        }
        public static void splitArticleContent(ref ArticleItem articleItem)
        {
            if (articleItem.DisplayFormat != 1)
            {
                Regex rx = null;
                Match m = null;

                //Article header Content---------------------------------
                rx = new Regex(@"<head[^>]*>(.*?)</head>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                m = rx.Match(articleItem.Content.CompleteContent);

                if (m.Success)
                {
                    articleItem.Content.HeaderContent = m.Groups[1].Value;
                }

                //After Header and before Body content--------------------
                rx = new Regex(@"</head[^>]*>(.*?)<body", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                m = rx.Match(articleItem.Content.CompleteContent);

                if (m.Success)
                {
                    articleItem.Content.AfterHeaderBeforeBodyContent = m.Groups[1].Value;
                }

                //Article Body Content------------------------------------
                articleItem.Content.BodyContent = articleItem.Content.CompleteContent;
                rx = new Regex(@"<body[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                m = rx.Match(articleItem.Content.CompleteContent);

                if (m.Success)
                {
                    articleItem.Content.BodyContent = m.Groups[1].Value;
                }

                //After Body Content---------------------------------------
                rx = new Regex(@"</body[^>]*>(.*?)</html>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                m = rx.Match(articleItem.Content.CompleteContent);

                if (m.Success)
                {
                    articleItem.Content.AfterBodyContent = m.Groups[1].Value;
                }
            }
        }
    }

    public static class Mime
    {
        public const string TextHtml = NetMime.Text.Html;
        public const string TextXml = NetMime.Text.Xml;
        public const string TextPlain = NetMime.Text.Plain;
        public const string TextRich = NetMime.Text.RichText;
        public const string TextCss = "text/css";
        public const string TextJavaScript = "text/javascript";

        public const string AppJson = "application/json";
        public const string AppOctetStream = NetMime.Application.Octet;
        public const string AppPdf = NetMime.Application.Pdf;
        public const string AppSoap = NetMime.Application.Soap;
        public const string AppZip = NetMime.Application.Zip;

        public const string ImagePng = "image/png";
        public const string ImageJpeg = NetMime.Image.Jpeg;
        public const string ImageGif = NetMime.Image.Gif;
        public const string ImageTiff = NetMime.Image.Tiff;
        public const string ImageBmp = "image/bmp";
        public const string ImageIco = "image/vnd.microsoft.icon"; // per IANA records

        public static string FromFilename(string filename) { return FromExtension(Path.GetExtension(filename)); }

        public static string FromExtension(string fileExt)
        {
            fileExt = fileExt.ToLower();
            if (fileExt.StartsWith("."))
                fileExt = fileExt.Substring(1);

            switch (fileExt.ToLower())
            {
                case "htm":
                case "html":
                case "xhtml":
                case "asp":
                case "aspx":
                case "php":
                    return TextHtml;
                case "jpeg":
                case "jpg":
                    return ImageJpeg;
                case "gif":
                    return ImageGif;
                case "png":
                    return ImagePng;
                case "bmp":
                    return ImageBmp;
                case "ico":
                    return ImageIco;
                case "tiff":
                    return ImageTiff;
                case "xml":
                    return TextXml;
                case "txt":
                    return TextPlain;
                case "css":
                    return TextCss;
                case "js":
                    return TextJavaScript;
                case "pdf":
                    return AppPdf;
                case "zip":
                    return AppZip;
                case "json":
                    return AppJson;
                case "exe":
                case "msi":
                case "dll":
                default:
                    return AppOctetStream;
            }
        }
    }
}