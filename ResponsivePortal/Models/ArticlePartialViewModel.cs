using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class ArticlePartialViewModel:BaseViewModel
    {
        public ArticleItem ArticleItem { get; set; }
        public List<ImageLinkViewModel> ImageLinks { get; set; }
        public List<BreadcrumbViewModel> Attributes { get; set; }
        public List<BreadcrumbViewModel> Categories { get; set; }
        public List<Attachments> Attachments { get; set; }
        public ArticleShareViewModel ShareItem { get; set; }
        public ArticleModule ArticleConfiguration { get; set; }
    }
}