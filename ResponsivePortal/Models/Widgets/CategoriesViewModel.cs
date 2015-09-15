using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class CategoriesViewModel : BaseViewModel
    {
        public string Title { get; set; }
        public List<CategoryViewModel> Categories { get; set; }
        public bool BrowseEnabled { get; set; }

    }
}
