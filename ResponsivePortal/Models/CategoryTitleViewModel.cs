using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class CategoryTitleViewModel : BaseViewModel
    {
        public string Icon { get; set; }
        public string SelectedIcon { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }        
        public int Id { get; set; }
    }
}