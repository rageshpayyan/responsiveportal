using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class SearchViewModel : BaseViewModel
    {
        public string Title { get; set; }
        public string SearchLabel { get; set; }
        public bool DisplaySearch = true;
        public string ImageUrl { get; set; }
    }
}
