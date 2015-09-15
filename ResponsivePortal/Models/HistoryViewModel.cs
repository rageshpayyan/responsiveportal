using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ResponsivePortal.Models
{
    public class HistoryViewModel
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public int SolutionFinderId { get; set; }
        public int ChoiceId{ get; set; }
    }
}