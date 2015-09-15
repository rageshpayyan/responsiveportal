using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class HomeViewModel : BaseViewModel
    {
        public SearchViewModel SearchViewModel { get; set; }
        public AttributesViewModel AttributesViewModel { get; set; }
        public FavoritesViewModel FavoritesViewModel { get; set; }
        public HotTopicsViewModel HotTopicsViewModel { get; set; }
        public CategoriesViewModel CategoriesViewModel { get; set; }
        public List<CustomMessageViewModel> CustomMessageViewModelList { get; set; }
        public List<ArticlesFromCategoryViewModel> ArticlesFromCategoryViewModelList { get; set; }        
    }
}