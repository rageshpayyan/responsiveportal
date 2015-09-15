using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class SolutionFinderViewModel : BaseViewModel
    {
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }
        public string Title { get; set; }
        public List<SolutionFinderTileViewModel> SolutionFinderTiles { get; set; }        
    }
}