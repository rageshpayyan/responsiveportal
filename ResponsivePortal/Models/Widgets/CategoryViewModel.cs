using System;
using System.Collections.Generic;
using PortalAPI.Models;
namespace ResponsivePortal.Models
{
    public class CategoryViewModel
    {
        public Category Category { get; set; }
        public List<CategoryBrowse> ContentItems { get; set; }
    }
}
