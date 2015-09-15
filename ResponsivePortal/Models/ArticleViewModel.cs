using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class ArticleViewModel: BaseViewModel
    {
        public int ArticleId { get; set; }
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }
        public ArticlePartialViewModel ArticlePartialViewModel { get; set; }
        public List<RelatedArticle> RelAnsweres { get; set; }
        public List<RelatedLink> RelLinks { get; set; }
        public string SearchText { get; set; }
        public int? RelativeArtilceParentId { get; set; }
        public Boolean FromWidget { get; set; }
    }
}