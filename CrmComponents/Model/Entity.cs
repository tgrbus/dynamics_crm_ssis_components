using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace CrmComponents.Model
{
    [DataContract]
    public class Entity
    {
        [DataMember]
        public string LogicalName { get; set; }
        [DataMember]
        public string EntitySetName { get; set; }
        [DataMember]
        public bool ValidForAdvancedFind { get; set; }
        [DataMember]
        public bool IsIntersect { get; set; }
        [DataMember]
        public string IdName { get; set; }

        [DataMember]
        public List<AlternateKey> AlternateKeys { get; set; } = new List<AlternateKey>();
        [IgnoreDataMember]
        public List<CrmAttribute> Attributes { get; set; }
        [DataMember]
        public Relationship IntersectRelationship { get; set; }
        public Entity() {
            //AlternateKeys = new List<AlternateKey>();
        }
    }
}
