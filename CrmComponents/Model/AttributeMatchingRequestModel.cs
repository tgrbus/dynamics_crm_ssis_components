using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrmComponents.Helpers.Enums;

namespace CrmComponents.Model
{
    public class AttributeMatchingRequestModel : OrganizationRequestModel {
        public string AttributeMatchDefaultValue { get; set; }
        public MatchingEnum AttributeMatchMultiple { get; set; }
        public MatchingEnum AttributeMatchNotFound { get; set; }
        public CrmColumn OriginalColumnForAttributeLookup { get; set; } = null;//holding reference for original column from DestinationComponent
        public int AttributeMatchUniqueIndex { get; set; }
        public string AttributeMatchUniqueString { get; set; }
        public string ConcatenatedColumnNames { get; set; }
    }
}
