using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CrmComponents.Model
{
    [DataContract]
    public class Relationship
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Entity1AttributeName { get; set; }
        [DataMember]
        public string Entity1EntityName { get; set; }
        [DataMember]
        public string Entity1EntitySetName { get; set; }
        [DataMember]
        public string Entity2AttributeName { get; set; }
        [DataMember]
        public string Entity2EntityName { get; set; }
        [DataMember]
        public string Entity2EntitySetName { get; set; }
    }
}
