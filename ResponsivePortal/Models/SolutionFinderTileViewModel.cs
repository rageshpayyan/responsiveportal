using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class SolutionFinderTileViewModel :BaseViewModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Content { get; set; }
        public int Id { get; set; }
    }
}