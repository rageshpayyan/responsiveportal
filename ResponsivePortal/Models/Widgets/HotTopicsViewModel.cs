using PortalAPI.Models;
using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class HotTopicsViewModel : BaseViewModel
    {
        public string  Title { get; set; }
        public int ArticlesCount { get; set; }
        public int Days { get; set; }
        public List<ArticleItem> ContentList { get; set; }
    }
}
