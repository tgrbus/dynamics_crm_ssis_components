using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    [DataContract]
    public class Lookup
    {
        [DataMember]
        public string TargetEntityName { get; set; }
        [DataMember]
        public string TargetEntitySetName { get; set; }
        [DataMember]
        public string LookupOdataName { get; set; }

    }
}
