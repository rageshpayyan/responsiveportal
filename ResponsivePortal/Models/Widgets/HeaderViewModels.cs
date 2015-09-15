using PortalAPI.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ResponsivePortal.Models
{
    public class HeaderViewModel : BaseViewModel
    {
        public List<SelectListItem> NavigationList { get; set; }
        public string BeforePortal {get;set;}

        public PortalType portalType;
    }
}
