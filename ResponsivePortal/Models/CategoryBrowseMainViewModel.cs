using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ResponsivePortal.Models
{
    public class CategoryBrowseMainViewModel : BaseViewModel
    {
        public List<CategoryBrowseViewModel> CategoryBrowseViewModel { get; set; }
        public categoryArticleViewModel CategoryBrowseArticleViewModel { get; set; }
        public CategoryBrowseHeaderViewModel CategoryBrowseHeaderViewModel { get; set; }
        public List<CategoryBrowseViewModel> AppliedCategories { get; set; }
        public int Page { get; set; }
        public int ResultsPerPage { get; set; }
        public int ParentcategorySelected { get; set; }
        public int LastcategoryIdSelected { get; set; }
        public string SearchImageUrl { get; set; }
        public ResultsDisplay ResultsDisplay { get; set; }
        [AllowHtml]
        public string SearchText { get; set; }

    }

    public class DictionaryOfcategoryArticleViewModel
    {
        public Dictionary<int, CategoryBrowseMainViewModel> DictioinaryOfcategoryArticleViewModel { get; set; }
    }

    public class DictionaryOfcategoryApplied
    {
        public Dictionary<int, List<CategoryBrowseViewModel>> CategoriesApplied { get; set; }
    }
    
}