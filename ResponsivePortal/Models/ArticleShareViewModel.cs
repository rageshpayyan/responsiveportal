using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ResponsivePortal.Models
{
    public class ArticleShareViewModel
    {
        public string ArticleShareUrl;
        public string EmailShareMessageBody;
        public List<ImageLinkViewModel> ShareLinkList;
    }
}