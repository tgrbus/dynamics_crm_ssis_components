using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrmComponents.Model
{
    public class ModifiedAttribute {
        public string Changed { get; set; } //new, updated, deleted
        public string LogicalName { get; set; }
        public string Type { get; set; }
    }
}
