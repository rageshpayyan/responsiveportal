using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class SolutionFinderDetailsViewModel : BaseViewModel
    {
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }
        public PortalAPI.Models.SolutionFinderChoice SolutionFinder { get; set; }
        public string NoChoicesMessage { get; set; }
        public int ImmediatePId { get; set; }
        public int ChoiceId { get; set; }
        public SolutionFinderTileViewModel SolutionFinderTileViewModel { get; set; }
        public List<HistoryViewModel> History { get; set; }
        public ArticlePartialViewModel ArticlePartialViewModel { get; set; }
        public Boolean FromWidget { get; set; }
        public int SearchId { get; set; }
    }
}