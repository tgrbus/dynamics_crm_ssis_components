using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using CrmComponents.Helpers.Enums;

//using System.Runtime.Serialization.Json;

namespace CrmComponents.Model
{
    [DataContract]
    public class EntityModel
    {
        [DataMember]
        public string ConnectionId { get; set; }
        [DataMember]
        public string ConnectionString { get; set; }

        [DataMember]
        public List<Entity> EntityList { get; set; } = new List<Entity>();
        [DataMember]
        public Entity SelectedEntity { get; set; }

        [DataMember]
        public List<CrmAttribute> Attributes { get; set; } = new List<CrmAttribute>();
        [DataMember]
        public List<SsisInput> SsisInputs { get; set; }
        [DataMember]
        public int BatchSize { get; set; }
        [DataMember]
        public List<string> MatchingColumns { get; set; }
        [DataMember]
        public string FetchXml { get; set; }
        [DataMember]
        public string Error { get; set; }
        
        public EntityModel() {
            this.BatchSize = 10;
        }

        public List<Entity> ReturnMergeEnities() {
            return new List<Entity>(new [] {
                new Entity{ LogicalName = "account" },
                new Entity{ LogicalName = "contact" },
                new Entity{ LogicalName = "incident" },
                new Entity{ LogicalName = "lead" }
            });
        }

        public List<CrmAttribute> ReturnMergeAttributes() {
            return new List<CrmAttribute>(new [] {
                new CrmAttribute { NotEntityAttribute = true, LogicalName = "PerformParentingChecks", CrmAttributeType = AttributeTypeEnum.Boolean},
                new CrmAttribute {NotEntityAttribute = true, LogicalName = "subordinateid", CrmAttributeType = AttributeTypeEnum.Uniqueidentifier },
                new CrmAttribute {NotEntityAttribute = true, LogicalName = "targetid", CrmAttributeType = AttributeTypeEnum.Uniqueidentifier }
            });
        }
    }
}
