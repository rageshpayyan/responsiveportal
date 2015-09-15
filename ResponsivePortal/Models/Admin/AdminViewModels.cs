using System.ComponentModel.DataAnnotations;
using PortalAPI.Models;
using System.Collections.Generic;
using System.Xml;

namespace ResponsivePortal.Models
{
    public class AdminLoginViewModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

    }

    public class PortalViewModel
    {
        public List<PS4> PortalPS4ViewModel { get; set; }
        public List<PS5> PortalPS5ViewModel { get; set; }
        
    }

    public class ConfigModel
    {
        public BreadcrumbViewModel BreadcrumbViewModel { get; set; }
        public string Title { get; set; }

        public int PortalId { get; set; }

        public int LangId { get; set; }
        public string RequestPath { get; set; }  

        public int ConfigType {get;set;}
        public string XmlContent { get; set; }

        public string TextContent { get; set; }
        public int CSSFileId { get; set; }
        public string ErrorMsg { get; set; }

        public Dictionary<int, string> CssFiles { get; set; }

        public List<FileList> ModifiedFilesModel { get; set; }
    }
}
