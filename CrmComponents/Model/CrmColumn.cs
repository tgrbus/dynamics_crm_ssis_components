using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;

namespace CrmComponents.Model
{
    public class CrmColumn {
        public string LogicalName { get; set; }
        public AttributeTypeEnum CrmType { get; set; }
        public object Value { get; set; }
        public string SssisName { get; set; }

        public Lookup LookupTarget { get; set; }

        //for complex lookup
        public ComplexMappingEnum ComplexMapping { get; set; } = ComplexMappingEnum.PrimaryKey;
        public object OriginalValue { get; set; }
        public List<CrmColumn> AlternateKeys { get; set; }
        public string StringValue { get; set; }
        public CrmAttribute CrmAttribute { get; set; }
    }
}
