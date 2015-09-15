using PortalAPI.Models;
using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class FavoritesViewModel : BaseViewModel
    {
        public string  Title { get; set; }
        public int ArticlesCount { get; set; }
        public int Days { get; set; }
        public List<FavoriteArticle> ContentList { get; set; }
        public bool MyFavorites { get; set; }
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }

    }
}
