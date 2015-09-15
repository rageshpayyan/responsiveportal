using PortalAPI.Models;
using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class ArticlesFromCategoryViewModel : BaseViewModel
    {
        public string  Title { get; set; }
        public string CategoryName { get; set; }
        public int ArticlesCount { get; set; }
        public int Days { get; set; }
        public int CategoryId { get; set; }
        public CatArticleItemBase ContentList { get; set; }
        public bool BrowseEnabled { get; set; }
        public string MoreText { get; set; }
    }

}
