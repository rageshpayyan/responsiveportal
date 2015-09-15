using System;
using System.Collections.Generic;

namespace ResponsivePortal.Models
{
    public class AttributesViewModel : BaseViewModel
    {
        public string Title { get; set; }
        public List<AttributeViewModel> AttributesList { get; set; }
    }
}
