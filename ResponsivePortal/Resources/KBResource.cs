using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Resources
{
    public class KBResource
    {
            public Dictionary<string, Dictionary<string, string>> Resources;
            public int PortalId;
            public int ClientId;
            public KBResource(int portalId, int clientId)
            {
                this.PortalId = portalId;
                this.ClientId = clientId;
            }
        }
    }
