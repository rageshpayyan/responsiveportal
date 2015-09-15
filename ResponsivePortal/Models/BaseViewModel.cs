using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class BaseViewModel
    {
        public int clientId;
        public int portalId;
        public Dictionary<string, string> Resources { get; set; }
        public string SessionTimeOutWarning { get; set; }
        public string SessionTimedOut { get; set; }
        public bool showBreadcrumb { get; set; }
        public bool isActiveDiretoryUser { get; set; }
    }
}