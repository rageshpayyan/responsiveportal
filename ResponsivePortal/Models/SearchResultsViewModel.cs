using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class SearchResultsViewModel
    {

        public int Id { get; set; }
        public int SFId { get; set; } // Solution Finder Id
        public int SFCId { get; set; } // Solution Finder choice Id
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Modified { get; set; }
        public string KBName { get; set; }
        public string Attributes { get; set; }
        public double Size { get; set; }
        public string SourceName { get; set; }
        public string SourceType {get;set;}
        public string FileType { get;set;}
    }

  public  enum SearchFrom
    {
        HomePage,
        SearchPage,
        ArticlePage,
        Paging,
        HomeWidget,
        SearchInsteadFor,
        Suggestion,
        SolutionFinder
    }
}