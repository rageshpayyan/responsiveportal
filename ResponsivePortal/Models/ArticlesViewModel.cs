using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ResponsivePortal.Models
{
    public class ArticlesViewModel111111111111111111111111111111
    {
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }
        public string Title { get; set; }
        public string Modified { get; set; }
        public string KBTitle { get; set; }
        public string KBNumber { get; set; }
        public string ArticleSize { get; set; }
        public List<SelectListItem> Attributes { get; set; }
        public List<SelectListItem> Categories { get; set; }
        public List<ImageLinkViewModel> ImageLinks { get; set; }
        public string ContentHTML { get; set; }
        public List<KeyValuePair<string, string>> Attachments { get; set; }
        public List<RelatedArticle> RelAnsweres { get; set; }
        public List<RelatedLink> RelLinks { get; set; }
    }
}