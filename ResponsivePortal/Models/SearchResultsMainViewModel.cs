using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ResponsivePortal.Models
{
    public  class SearchResultsMainViewModel : BaseViewModel
    {
        [AllowHtml]
        public string Searchtext { get; set; }
        public int Page { get; set; }
        public  List<SearchResultsViewModel> SearchResultsViewModel { get; set; }
        public List<SearchResultRefinement> KnowledgeBases { get; set; }
        public List<SearchResultRefinement> Categories { get; set; }
        public List<SearchResultRefinement> Attributes { get; set; }
        public List<SearchResultContentTypeRefinement> ContentTypes { get; set; }
        public List<SearchResultFormatRefinement> Format { get; set; }
        public List<string> SuggestionSearch { get; set; }
        public List<string> SpellingCorrection { get; set; }
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }       
        public string ModifiedSearchText { get; set; }
        public int SearchResultsPerPage { get; set; }
        public string SearchImageUrl { get; set; }
        public FilterDisplayValues FilterDisplay { get; set; }
        public ResultsDisplay ResultsDisplay { get; set; }
        public Boolean FromWidget { get; set; }
        public int SearchId { get; set; }
    }
       

    public class FilterDisplayValues
    {
      public  bool Kb { get; set; }
      public bool Categories { get; set; }
      public bool Attributes { get; set; }
      public bool ContentTypes { get; set; }
      public bool Formats { get; set; }
    }
}