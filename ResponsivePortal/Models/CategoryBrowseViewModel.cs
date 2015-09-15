using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace ResponsivePortal.Models
{
    public class CategoryBrowseViewModel 
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ChildrenCount { get; set; }
    }

    public class categoryArticleViewModel 
    {
        public List<CatArticleItem> CatArticleItem { get; set; }
    }
}