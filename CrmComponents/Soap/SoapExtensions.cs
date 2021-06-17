using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;


namespace CrmComponents.Discovery_92_Online {
    public partial class DiscoveryServiceClient : CrmComponents.Soap.Extensions.IServiceClient { }
}

namespace CrmComponents.Organization_91_Online {

    public partial class OrganizationServiceClient : CrmComponents.Soap.Extensions.IServiceClient { }
    public partial class DiscoveryServiceClient : CrmComponents.Soap.Extensions.IServiceClient { }

    [KnownType(typeof(RetrieveAllEntitiesRequest))]
    [KnownType(typeof(RetrieveEntityRequest))]
    [KnownType(typeof(RetrieveMultipleRequest))]
    [KnownType(typeof(EntityFilters))]
    [KnownType(typeof(QueryExpression))]
    [KnownType(typeof(ColumnSet))]
    [KnownType(typeof(PagingInfo))]
    [KnownType(typeof(FetchExpression))]
    [KnownType(typeof(WhoAmIRequest))]
    [KnownType(typeof(ExecuteMultipleRequest))]
    [KnownType(typeof(OrganizationRequestCollection))]
    [KnownType(typeof(CreateRequest))]
    [KnownType(typeof(UpdateRequest))]
    [KnownType(typeof(UpsertRequest))]
    [KnownType(typeof(AssociateRequest))]
    [KnownType(typeof(DisassociateRequest))]
    [KnownType(typeof(DeleteRequest))]
    [KnownType(typeof(Entity))]
    [KnownType(typeof(EntityReference))]
    [KnownType(typeof(EntityReferenceCollection))]
    [KnownType(typeof(QueryBase))]
    [KnownType(typeof(FilterExpression))]
    [KnownType(typeof(FilterExpression[]))]
    [KnownType(typeof(ColumnSet[]))]
    [KnownType(typeof(ConditionExpression))]
    [KnownType(typeof(ConditionExpression[]))]
    [KnownType(typeof(MergeRequest))]
    public partial class OrganizationRequest { }

    [KnownType(typeof(RetrieveAllEntitiesResponse))]
    [KnownType(typeof(RetrieveEntityResponse))]
    [KnownType(typeof(RetrieveMultipleResponse))]
    [KnownType(typeof(EntityMetadata[]))]
    [KnownType(typeof(EntityMetadata))]
    [KnownType(typeof(EntityCollection))]
    [KnownType(typeof(OptionSetValue))]
    [KnownType(typeof(Money))]
    [KnownType(typeof(EntityReference))]
    [KnownType(typeof(WhoAmIResponse))]
    [KnownType(typeof(ExecuteMultipleResponse))]
    [KnownType(typeof(CreateResponse))]
    [KnownType(typeof(UpdateResponse))]
    [KnownType(typeof(UpsertResponse))]
    [KnownType(typeof(AssociateResponse))]
    [KnownType(typeof(DisassociateResponse))]
    [KnownType(typeof(DeleteResponse))]
    [KnownType(typeof(ManyToManyRelationshipMetadata[]))]
    [KnownType(typeof(ManyToManyRelationshipMetadata))]
    
    [KnownType(typeof(MergeResponse))]
    public partial class OrganizationResponse {}

    [KnownType(typeof(OptionSetValue))]
    [KnownType(typeof(OptionSetValueCollection))]
    [KnownType(typeof(Money))]
    [KnownType(typeof(EntityReference))]
    [KnownType(typeof(AliasedValue))]
    public partial class EntityCollection { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveAllEntitiesRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveAllEntitiesRequest : OrganizationRequest { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveAllEntitiesResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveAllEntitiesResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveEntityRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveEntityRequest : OrganizationRequest { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveEntityResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveEntityResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveMultipleRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveMultipleRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private QueryBase QueryField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public QueryBase Query
        {
            get { return this.QueryField; }
            set {
                if (object.ReferenceEquals(this.QueryField, value) != true) {
                    this.QueryField = value;
                    this.RaisePropertyChanged("Query");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "RetrieveMultipleResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class RetrieveMultipleResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "WhoAmIRequest", Namespace = "http://schemas.microsoft.com/crm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class WhoAmIRequest : OrganizationRequest { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "WhoAmIResponse", Namespace = "http://schemas.microsoft.com/crm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class WhoAmIResponse : OrganizationResponse, CrmComponents.Soap.Extensions.IResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "ExecuteMultipleRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class ExecuteMultipleRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private OrganizationRequestCollection RequestsField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private ExecuteMultipleSettings SettingsField;

        [System.Runtime.Serialization.DataMemberAttribute]
        public OrganizationRequestCollection Requests {
            get { return this.RequestsField; }
            set {
                if((object.ReferenceEquals(this.RequestsField, value) != true)) {
                    this.RequestsField = value;
                    this.RaisePropertyChanged("Requests");
                }
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public ExecuteMultipleSettings Settings {
            get { return this.SettingsField; }
            set {
                if(object.ReferenceEquals(this.SettingsField, value) != true) {
                    this.SettingsField = value;
                    this.RaisePropertyChanged("Settings");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "ExecuteMultipleResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class ExecuteMultipleResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContract]
    [System.SerializableAttribute()]
    public class CreateRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Entity TargetField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public Entity Target {
            get { return this.TargetField; }
            set {
                if(object.ReferenceEquals(this.TargetField, value) != true) {
                    this.TargetField = value;
                    this.RaisePropertyChanged("Target");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "CreateResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class CreateResponse : OrganizationResponse {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Guid idField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public Guid id {
            get { return this.idField; }
            set {
                if(object.ReferenceEquals(this.idField, value) != true) {
                    this.idField = value;
                    this.RaisePropertyChanged("id");
                }}
        }
    }

    public class UpdateRequest : CreateRequest { }
    public class UpsertRequest : CreateRequest { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "AssociateRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class AssociateRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private EntityReference TargetField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Relationship RelationshipField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private EntityReferenceCollection RelatedEntitiesField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public EntityReference Target {
            get { return this.TargetField; }
            set { 
                if (object.ReferenceEquals(this.TargetField, value) != true) {
                    this.TargetField = value;
                    this.RaisePropertyChanged("Target");
                }
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public Relationship Relationship {
            get { return this.RelationshipField; }
            set {
                if(object.ReferenceEquals(this.RelationshipField, value) != true) {
                    this.RelationshipField = value;
                    this.RaisePropertyChanged("Relationship");
                }
            }
        }

        public EntityReferenceCollection RelatedEntities {
            get { return this.RelatedEntitiesField; }
            set {
                if(object.ReferenceEquals(this.RelatedEntitiesField, value) != true) {
                    this.RelatedEntitiesField = value;
                    this.RaisePropertyChanged("RelatedEntities");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "DisassociateRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class DisassociateRequest : AssociateRequest { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "MergeRequest", Namespace = "http://schemas.microsoft.com/crm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class MergeRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private EntityReference TargetField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Guid SubordinateIdField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private bool PerformParentingChecksField;

        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Entity UpdateContentField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public EntityReference Target {
            get { return this.TargetField; }
            set {
                if (object.ReferenceEquals(this.TargetField, value) != true) {
                    this.TargetField = value;
                    this.RaisePropertyChanged("Target");
                }
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public Guid SubordinateId {
            get { return this.SubordinateIdField; }
            set {
                if(this.SubordinateIdField != value) {
                    this.SubordinateIdField = value;
                    this.RaisePropertyChanged("SubordinateId");
                }
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public bool PerformParentingChecks {
            get { return this.PerformParentingChecksField; }
            set {
                if(this.PerformParentingChecksField != value) {
                    this.PerformParentingChecksField = value;
                    this.RaisePropertyChanged("PerformParentingChecks");
                }
            }
        }

        public Entity UpdateContent {
            get { return this.UpdateContentField; }
            set {
                if(object.ReferenceEquals(this.UpdateContentField, value) != true) {
                    this.UpdateContentField = value;
                    this.RaisePropertyChanged("UpdateContent");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "DeleteRequest", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class DeleteRequest : OrganizationRequest {
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private EntityReference TargetField;

        [System.Runtime.Serialization.DataMemberAttribute()]
        public EntityReference Target {
            get { return this.TargetField; }
            set {
                if (object.ReferenceEquals(this.TargetField, value) != true) {
                    this.TargetField = value;
                    this.RaisePropertyChanged("Target");
                }
            }
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "UpdateResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class UpdateResponse : CreateResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "UpsertResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class UpsertResponse : CreateResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "AssociateResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class AssociateResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "DisassociateResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class DisassociateResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "DeleteResponse", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class DeleteResponse : OrganizationResponse { }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "MergeResponse", Namespace = "http://schemas.microsoft.com/crm/2011/Contracts")]
    [System.SerializableAttribute()]
    public class MergeResponse : OrganizationResponse { }

    public partial class EntityCollection : CrmComponents.Soap.Extensions.IResponse { }
}

namespace CrmComponents.Soap.Extensions {
    public interface IServiceClient { }

    public interface IResponse { }
}
