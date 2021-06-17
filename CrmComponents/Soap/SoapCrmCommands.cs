using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CrmComponents.Helpers;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;
using CrmComponents.Organization_91_Online;
using EndpointType = CrmComponents.Discovery_92_Online.EndpointType;
using Entity = CrmComponents.Model.Entity;

namespace CrmComponents.Soap
{
    public class SoapCrmCommands : CrmCommands {
        public DateTime expires = new DateTime(1900, 01, 01);
        public Connection connection;
        public Connection Connection {
            get { return connection; }
            set {
                if(value.ConnectionType == ConnectionTypeEnum.Adfs || value.ConnectionType == ConnectionTypeEnum.Online) {
                    expires = new DateTime(1900, 1, 1);
                }

                connection = value;
            }
        }

        private string url;
        private string userName;
        private string password;

        private DataContractJsonSerializer serializer;

        private TransportLayer transportLayer;
        
        public SoapCrmCommands(string connectionString) : this() {
            this.connection = new Connection(connectionString);            
        }

        public SoapCrmCommands(Connection connection) : this() {
            this.connection = connection;            
        }

        private SoapCrmCommands() {
            this.transportLayer = new TransportLayer(this);
        }


        public override void FindRecordIdByMatching(List<AttributeMatchingRequestModel> requests, int batchSize = 10) {
            for (int i = 0; i < requests.Count; i += batchSize) {
                int reqNum = 0;
                ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest {
                    RequestName = "ExecuteMultiple",
                    Parameters = new ParameterCollection()
                };
                ExecuteMultipleSettings settings = new ExecuteMultipleSettings {
                    ContinueOnError = true,
                    ReturnResponses = true
                };
                OrganizationRequestCollection collection = new OrganizationRequestCollection();
                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Settings", settings));
                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Requests", collection));
                List<AttributeMatchingRequestModel> requestSub = requests.Skip(i).Take(batchSize).ToList();
                foreach(OrganizationRequestModel r in requestSub) {
                    List<CrmAttribute> matchingColumns = r.Row.Columns.Select(n => n.CrmAttribute).OrderBy(n => n.LogicalName).ToList();
                    RetrieveMultipleRequest req = new RetrieveMultipleRequest();
                    r.RequestIndex = reqNum++;
                    req.RequestName = "RetrieveMultiple";
                    QueryExpression qe = new QueryExpression();
                    qe.EntityName = r.Entity.LogicalName;
                    qe.ColumnSet = new ColumnSet();
                    qe.ColumnSet.AllColumns = false;
                    qe.ColumnSet.Columns = new List<string>().ToArray();
                    FilterExpression fe = new FilterExpression();
                    fe.Conditions = new ConditionExpression[matchingColumns.Count];
                    for(int j = 0; j < matchingColumns.Count; j++) {
                        CrmAttribute a = matchingColumns[j];
                        CrmColumn c = r.Row.Columns.First(n => n.LogicalName == a.LogicalName);
                        ConditionExpression ce = new ConditionExpression();
                        ce.AttributeName = a.LogicalName;
                        ce.Operator = ConditionOperator.Equal;
                        ce.Values = new object[1];
                        if(a.MatchingDefaultValue != null) {
                            if(a.MatchingDefaultValue.Trim() == "" || a.MatchingDefaultValue.Trim().ToLower() == "null") {
                                c.Value = null;
                            } else {
                                c.Value = a.MatchingDefaultValue;
                            }
                        }
                        ce.Values[0] = ReturnMatchingValue(c);
                        fe.Conditions[j] = ce;

                        r.Row.Columns.Remove(c);
                    }

                    qe.Criteria = fe;
                    qe.PageInfo = new PagingInfo();
                    qe.PageInfo.PageNumber = 0;
                    qe.PageInfo.PagingCookie = null;
                    req.Parameters = new ParameterCollection();
                    req.Parameters.Add(new KeyValuePair<string, object>("Query", qe));
                    collection.Add(req);
                }

                ExecuteMultipleResponse response;
                if (connection.ConnectionType == ConnectionTypeEnum.Ad) {
                    OrganizationServiceClient adClient = (OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint, timeoutSec: connection.TimeoutSec.Value);
                    response = (ExecuteMultipleResponse)adClient.Execute(multipleRequest);
                }
                else {
                    response = (ExecuteMultipleResponse)ExecuteSoapRequestOrganizationEndpoint<ExecuteMultipleRequest, ExecuteMultipleResponse>(multipleRequest, typeof(ExecuteMultipleResponse), MethodNameEnum.Execute, timeoutSec: connection.TimeoutSec.Value);
                }

                foreach(ExecuteMultipleResponseItem item in (OrganizationResponseCollection1)response.Results.First(n => n.Key == "Responses").Value) {
                    OrganizationResponseModel responseModel = new OrganizationResponseModel();
                    responseModel.Success = item.Fault == null;
                    responseModel.ResponseIndex = item.RequestIndex;
                    if(!responseModel.Success) {
                        responseModel.ErrorMessage = item.Fault?.Message;
                    }

                    responseModel.ComputedGuids = new List<Guid>();
                    if(responseModel.Success) {
                        responseModel.ComputedGuids = ((EntityCollection)item.Response.Results.First(n => n.Key == "EntityCollection").Value).Entities.Select(n => n.Id).ToList();
                    }

                    requestSub.First(n => n.RequestIndex == responseModel.ResponseIndex).Response = responseModel;
                }
            }
        }

        private object ReturnMatchingValue(CrmColumn c) {
            object value = null;
            if(c.Value != null) {
                switch(c.CrmType) {
                    case AttributeTypeEnum.String:
                    case AttributeTypeEnum.Memo:
                        value = c.Value.ToString();
                        break;
                    case AttributeTypeEnum.DateTime:
                        if(c.Value.GetType() != typeof(DateTime)) {
                            c.Value = DateTime.Parse(c.Value.ToString());
                        }
                        value = (DateTime)c.Value;
                        break;
                    case AttributeTypeEnum.Lookup:
                    case AttributeTypeEnum.Customer:
                    case AttributeTypeEnum.Owner:
                    case AttributeTypeEnum.Uniqueidentifier:
                        if(c.Value.GetType() != typeof(Guid)) {
                            c.Value = new Guid(c.Value.ToString());
                        }
                        value = (Guid)c.Value;
                        break;
                    case AttributeTypeEnum.Decimal:
                    case AttributeTypeEnum.Money:
                        if(c.Value.GetType() != typeof(decimal)) {
                            c.Value = decimal.Parse(c.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }
                        value = (decimal)c.Value;
                        break;
                    case AttributeTypeEnum.State:
                    case AttributeTypeEnum.Status:
                    case AttributeTypeEnum.Picklist:
                        int? optionValue = AttributeHelpers.SetOptionsetValue(c);
                        if(optionValue == null) {
                            throw new Exception("Can't find optionset value for " + c.Value.ToString());
                        }

                        value = optionValue.Value;
                        break;
                    case AttributeTypeEnum.Integer:
                        if(c.Value.GetType() != typeof(int)) {
                            c.Value = int.Parse(c.Value.ToString());
                        }
                        value = (int)c.Value;
                        break;
                    case AttributeTypeEnum.Boolean:
                        if (c.Value.GetType() != typeof(bool)) {
                            c.Value = c.Value is int ? Convert.ToBoolean(c.Value) : bool.Parse(c.Value.ToString());
                        }
                        value = (bool)c.Value;
                        break;
                    case AttributeTypeEnum.Double:
                        if(c.Value.GetType() != typeof(double)) {
                            c.Value = double.Parse(c.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }
                        value = (double)c.Value;
                        break;
                    default:
                        throw new Exception("Can't cast matching column to crm type");
                }
            }

            return value;
        }

        public override void Merge(Entity mergeEntity, List<OrganizationRequestModel> requests, int batch = 5,
                                   bool continueOnError = true, bool returnResponses = true) {
            for(int i = 0; i < requests.Count; i += batch) {
                int reqNum = 0;
                ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest {
                    RequestName = "ExecuteMultiple",
                    Parameters = new ParameterCollection()
                };
                OrganizationRequestCollection collection = new OrganizationRequestCollection();
                ExecuteMultipleSettings settings = new ExecuteMultipleSettings {
                    ContinueOnError = continueOnError,
                    ReturnResponses = returnResponses
                };
                List<OrganizationRequestModel> requestsSub = requests.Skip(i).Take(batch).ToList();
                foreach(OrganizationRequestModel r in requestsSub) {
                    Organization_91_Online.OrganizationRequest req = new MergeRequest {RequestName = "Merge"};
                    CrmColumn targetColumn = r.Row.Columns.First(n => n.LogicalName == "targetid");
                    if (targetColumn.Value.GetType() != typeof(Guid)) {
                        targetColumn.Value = Guid.Parse(targetColumn.Value.ToString());
                    }
                    Guid targetId = ((Guid?)targetColumn.Value).Value;
                    EntityReference targetReference = new EntityReference { Id = targetId, LogicalName = mergeEntity.LogicalName };
                    CrmColumn subordinateColumn = r.Row.Columns.First(n => n.LogicalName == "subordinateid");
                    if(subordinateColumn.Value.GetType() != typeof(Guid)) {
                        subordinateColumn.Value = Guid.Parse(subordinateColumn.Value.ToString());
                    }
                    Guid subordinateId = ((Guid?)subordinateColumn.Value).Value;
                    CrmColumn parentingChecksColumn = r.Row.Columns.FirstOrDefault(n => n.LogicalName == "PerformParentingChecks");
                    bool parentingChecks = false;
                    if (parentingChecksColumn.Value != null) {
                        if (parentingChecksColumn.Value.GetType() != typeof(bool)) {
                            parentingChecks = parentingChecksColumn.Value is int ? Convert.ToBoolean(parentingChecksColumn.Value) : bool.Parse(parentingChecksColumn.Value.ToString());
                        }
                        else {
                            parentingChecks = (bool)parentingChecksColumn.Value;
                        }
                    }

                    Organization_91_Online.Entity entity = new Organization_91_Online.Entity();
                    entity.LogicalName = mergeEntity.LogicalName;

                    req.Parameters = new ParameterCollection {
                        new KeyValuePair<string, object>("Target", targetReference),
                        new KeyValuePair<string, object>("SubordinateId", subordinateId),
                        new KeyValuePair<string, object>("PerformParentingChecks", parentingChecks),
                        new KeyValuePair<string, object>("UpdateContent", entity)
                    };
                    collection.Add(req);
                    r.RequestIndex = (reqNum++);
                }
                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Settings", settings));
                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Requests", collection));

                ExecuteMultipleResponse response;
                if (connection.ConnectionType == ConnectionTypeEnum.Ad) {
                    OrganizationServiceClient adClient = (OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint, timeoutSec: connection.TimeoutSec.Value);
                    response = (ExecuteMultipleResponse)adClient.Execute(multipleRequest);
                }
                else {
                    response = (ExecuteMultipleResponse)ExecuteSoapRequestOrganizationEndpoint<ExecuteMultipleRequest, ExecuteMultipleResponse>(multipleRequest, typeof(ExecuteMultipleResponse), MethodNameEnum.Execute, timeoutSec: connection.TimeoutSec.Value);
                }

                foreach(ExecuteMultipleResponseItem resp in ((OrganizationResponseCollection1)response.Results.First(n => n.Key == "Responses").Value)) {
                    OrganizationResponseModel responseModel = new OrganizationResponseModel();
                    responseModel.Success = resp.Fault == null;
                    
                    responseModel.ResponseIndex = resp.RequestIndex;
                    if (!responseModel.Success) {
                        responseModel.ErrorMessage = resp.Fault?.Message;
                    }
                    requestsSub.First(n => n.RequestIndex == responseModel.ResponseIndex).Response = responseModel;
                }
            }
        }

        public override void OrganizationReq(OperationTypeEnum operationType, ComplexMappingEnum idMapping, List<OrganizationRequestModel> requests, 
                                                 int batch = 5, bool continueOnError = true, bool returnResponses = true, 
                                                 bool ignoreNullValuedFields = false, Model.Relationship relationship = null) {
            
            for (int i = 0; i < requests.Count; i += batch) {
                int reqNum = 0;
                ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest();
                multipleRequest.RequestName = "ExecuteMultiple";
                multipleRequest.Parameters = new ParameterCollection();

                OrganizationRequestCollection collection = new OrganizationRequestCollection();
                ExecuteMultipleSettings settings = new ExecuteMultipleSettings {
                    ContinueOnError = continueOnError,
                    ReturnResponses = returnResponses
                };
                List<OrganizationRequestModel> requestsSub = requests.Skip(i).Take(batch).ToList();
                foreach(OrganizationRequestModel r in requestsSub) {
                    Organization_91_Online.OrganizationRequest req;

                    if (operationType == OperationTypeEnum.Delete) {
                        EntityReference er = new EntityReference();
                        er.LogicalName = r.Entity.LogicalName;
                        if (idMapping == ComplexMappingEnum.PrimaryKey || idMapping == ComplexMappingEnum.Manual) {
                            er.Id = r.PrimaryId.Value;
                        }
                        else if (idMapping == ComplexMappingEnum.AlternateKey) {
                            er.KeyAttributes = new KeyAttributeCollection();
                            foreach (CrmColumn key in r.AlternateKeys) {
                                KeyValuePair<string, object> k = new KeyValuePair<string, object>(key.LogicalName, SetAttributeValue(key));
                                er.KeyAttributes.Add(k);
                            }
                        }

                        DeleteRequest dr = new DeleteRequest();
                        dr.RequestName = "Delete";
                        dr.Parameters = new ParameterCollection();
                        dr.Parameters.Add(new KeyValuePair<string, object>("Target", er));
                        collection.Add(dr);
                    }
                    else if(operationType == OperationTypeEnum.Associate || operationType == OperationTypeEnum.Disassociate) {
                        var ar = new AssociateRequest();
                        CrmColumn c1 = r.Row.Columns.First(n => n.LogicalName == relationship.Entity1AttributeName);
                        c1.Value = SetAttributeValue(c1); //Target entity reference
                        CrmColumn c2 = r.Row.Columns.First(n => n.LogicalName == relationship.Entity2AttributeName);
                        c2.Value = SetAttributeValue(c2);
                        EntityReference related = (EntityReference)c2.Value;
                        EntityReferenceCollection relatedCollection = new EntityReferenceCollection();
                        relatedCollection.Add(related);

                        Organization_91_Online.Relationship rel = new Organization_91_Online.Relationship();
                        rel.SchemaName = relationship.Name;

                        ar.RequestName = "Associate";
                        ar.Parameters = new ParameterCollection();
                        ar.Parameters.Add(new KeyValuePair<string, object>("Target", c1.Value));
                        ar.Parameters.Add(new KeyValuePair<string, object>("RelatedEntities", relatedCollection));
                        ar.Parameters.Add(new KeyValuePair<string, object>("Relationship", rel));

                        if(operationType == OperationTypeEnum.Disassociate) {
                            var dr = new DisassociateRequest();
                            dr.RequestName = "Disassociate";
                            dr.Parameters = ar.Parameters;
                            collection.Add(dr);
                        } else {
                            collection.Add(ar);
                        }
                    }
                    else {//create, update, upsert
                        Organization_91_Online.Entity entity = ComposeEntity(r, ignoreNullValuedFields);
                        if(operationType == OperationTypeEnum.Create) {
                            req = new CreateRequest();
                            req.RequestName = "Create";
                        } else if(operationType == OperationTypeEnum.Update) {
                            req = new UpdateRequest();
                            req.RequestName = "Update";
                        } else {
                            req = new UpsertRequest();
                            req.RequestName = "Upsert";
                        }

                        if(idMapping == ComplexMappingEnum.PrimaryKey && (operationType == OperationTypeEnum.Update || operationType == OperationTypeEnum.Upsert)) {
                            entity.Id = r.PrimaryId.Value;
                        }
                        else if(idMapping == ComplexMappingEnum.AlternateKey && (operationType == OperationTypeEnum.Update || operationType == OperationTypeEnum.Upsert)) {
                            entity.KeyAttributes = new KeyAttributeCollection();
                            foreach(CrmColumn key in r.AlternateKeys) {
                                KeyValuePair<string, object> k = new KeyValuePair<string, object>(key.LogicalName, SetAttributeValue(key));
                                entity.KeyAttributes.Add(k);
                            }
                        }
                        else if(idMapping == ComplexMappingEnum.Manual && (operationType == OperationTypeEnum.Update || operationType == OperationTypeEnum.Upsert)) {
                            entity.Id = r.PrimaryId.Value;
                        }

                        req.Parameters = new ParameterCollection();
                        req.Parameters.Add(new KeyValuePair<string, object>("Target", entity));
                        req.Parameters.Add(new KeyValuePair<string, object>("SuppressDuplicateDetection", true));
                        collection.Add(req);
                    } 
                    
                    r.RequestIndex = (reqNum++);
                }

                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Settings", settings));
                multipleRequest.Parameters.Add(new KeyValuePair<string, object>("Requests", collection));

                ExecuteMultipleResponse response;
                if(connection.ConnectionType == ConnectionTypeEnum.Ad) {
                    OrganizationServiceClient adClient = (OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint, timeoutSec: connection.TimeoutSec.Value);
                    response = (ExecuteMultipleResponse)adClient.Execute(multipleRequest);
                } else {
                    response = (ExecuteMultipleResponse)ExecuteSoapRequestOrganizationEndpoint<ExecuteMultipleRequest, ExecuteMultipleResponse>(multipleRequest, typeof(ExecuteMultipleResponse), MethodNameEnum.Execute, timeoutSec:connection.TimeoutSec.Value);
                }

                foreach(ExecuteMultipleResponseItem resp in ((OrganizationResponseCollection1)response.Results.First(n => n.Key == "Responses").Value)) {
                    OrganizationResponseModel responseModel = new OrganizationResponseModel();
                    responseModel.Success = resp.Fault == null;
                    Guid? id = null;
                    if(operationType == OperationTypeEnum.Create) {
                        id = (Guid?)resp.Response?.Results?.FirstOrDefault(n => n.Key == "id").Value;
                    }
                    else if(operationType == OperationTypeEnum.Upsert) {
                        var r = resp.Response?.Results;
                        id = ((EntityReference)r?.FirstOrDefault(n => n.Key == "Target").Value)?.Id;
                        responseModel.RecordCreated = (bool?)r?.FirstOrDefault(n => n.Key == "RecordCreated").Value;
                    }

                    var ii = resp.RequestIndex;
                    responseModel.Id = id;
                    responseModel.ResponseIndex = ii;
                    if(!responseModel.Success) {
                        responseModel.ErrorMessage = resp.Fault?.Message;
                    }
                    requestsSub.First(n => n.RequestIndex == ii).Response = responseModel;
                }
            }
        }

        private Organization_91_Online.Entity ComposeEntity(OrganizationRequestModel r, bool ignoreNullValuedFields = false) {
            Organization_91_Online.Entity entity = new Organization_91_Online.Entity();
            entity.LogicalName = r.Entity.LogicalName;
            entity.Attributes = new AttributeCollection();
            foreach (CrmColumn col in r.Row.Columns) {
                string key = col.LogicalName;
                object value = null;
                if(col.Value != null || col.ComplexMapping == ComplexMappingEnum.AlternateKey) {
                    value = SetAttributeValue(col);
                }
                KeyValuePair<string, object> c = new KeyValuePair<string, object>(key,value);
                if(col.Value != null || !ignoreNullValuedFields) {
                    entity.Attributes.Add(c);
                }
            }

            return entity;
        }

        private object SetAttributeValue(CrmColumn col) {
            object value = null;
            try {
                switch (col.CrmType) {
                    case AttributeTypeEnum.Customer:
                    case AttributeTypeEnum.Lookup:
                    case AttributeTypeEnum.Owner:
                        EntityReference er = new EntityReference();
                        er.LogicalName = col.LookupTarget.TargetEntityName;
                        if(col.ComplexMapping == ComplexMappingEnum.AlternateKey) {
                            er.KeyAttributes = new KeyAttributeCollection();
                            foreach(CrmColumn colAlternateKey in col.AlternateKeys) {
                                var v = colAlternateKey.Value != null ? SetAttributeValue(colAlternateKey) : null;
                                er.KeyAttributes.Add(new KeyValuePair<string, object>(colAlternateKey.LogicalName, v));
                            }
                        } else {
                            if(col.Value.GetType() != typeof(Guid)) {
                                col.Value = new Guid(col.Value.ToString());
                            }

                            er.Id = ((Guid?)col.Value).Value;
                        }

                        value = er;
                        break;
                    case AttributeTypeEnum.Picklist:
                    case AttributeTypeEnum.State:
                    case AttributeTypeEnum.Status:
                        OptionSetValue osv = new OptionSetValue();
                        int? optionValue = AttributeHelpers.SetOptionsetValue(col);
                        if(optionValue == null) {
                            throw new Exception("Can't find optionset value for " + col.Value.ToString());
                        }

                        osv.Value = optionValue.Value;
                        value = osv;
                        break;
                    case AttributeTypeEnum.BigInt:
                        if(col.Value.GetType() != typeof(long)) {
                            col.Value = long.Parse(col.Value.ToString());
                        }

                        long bi = (long)col.Value;
                        value = bi;
                        break;
                    case AttributeTypeEnum.Boolean:
                        if(col.Value.GetType() != typeof(bool)) {
                            col.Value = col.Value is int ? Convert.ToBoolean(col.Value) : bool.Parse(col.Value.ToString());
                        }

                        bool b = (bool)col.Value;
                        value = b;
                        break;
                    case AttributeTypeEnum.DateTime:
                        if(col.Value.GetType() != typeof(DateTime)) {
                            col.Value = DateTime.Parse(col.Value.ToString());
                        }

                        DateTime dt = (DateTime)col.Value;
                        value = dt;
                        break;
                    case AttributeTypeEnum.Decimal:
                        if(col.Value.GetType() != typeof(decimal)) {
                            col.Value = decimal.Parse(col.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }

                        decimal d = (decimal)col.Value;
                        value = d;
                        break;
                    case AttributeTypeEnum.Double:
                        if(col.Value.GetType() != typeof(double)) {
                            col.Value = double.Parse(col.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }

                        double dou = (double)col.Value;
                        value = dou;
                        break;
                    case AttributeTypeEnum.Integer:
                        if(col.Value.GetType() != typeof(int)) {
                            col.Value = int.Parse(col.Value.ToString());
                        }

                        int i = (int)col.Value;
                        value = i;
                        break;
                    case AttributeTypeEnum.Memo:
                    case AttributeTypeEnum.String:
                    case AttributeTypeEnum.PartyList:
                        string s = col.Value.ToString();
                        value = s;
                        break;
                    case AttributeTypeEnum.Money:
                        if(col.Value.GetType() != typeof(decimal)) {
                            col.Value = decimal.Parse(col.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }

                        Money m = new Money();
                        m.Value = (decimal)col.Value;
                        value = m;
                        break;
                    case AttributeTypeEnum.MultiSelectPicklist:
                        string[] ar = col.Value.ToString().Split(',', ' ');
                        OptionSetValue[] osva = new OptionSetValue[ar.Length];
                        for(int k = 0; k < ar.Length; k++) {
                            osva[k] = new OptionSetValue();
                            osva[k].Value = int.Parse(ar[k]);
                        }

                        value = osva;
                        break;
                    case AttributeTypeEnum.Uniqueidentifier:
                        if(col.Value.GetType() != typeof(Guid)) {
                            col.Value = Guid.Parse(col.Value.ToString());
                        }

                        value = ((Guid?)col.Value).Value;
                        break;
                    default:
                        throw new Exception("Attribute type not found");
                }
            } catch(Exception ex) {
                string message = $"{col.CrmAttribute.LogicalName}/{col.Value}";
                throw new Exception($"{message}/{ex.Message}/{ex.StackTrace}");
            }

            return value;
        }

        public override FetchBatch RetrieveAllRecords(string entityName, string entitySetName, List<CrmAttribute> attributes, List<CrmAttribute> additionalAttributes = null, int pageNumber = 1, string cookie = null, int count = 1000, bool allColumns = false) {
            var query = new QueryExpression();

            var columnSet = new ColumnSet();
            if(allColumns) {
                columnSet.AllColumns = true;
                columnSet.Columns = new string[0];
            } else {
                var columns = attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf)).Select(k => k.LogicalName).ToList();
                if(additionalAttributes != null && additionalAttributes.Count > 0) {
                    var additionalColumns = additionalAttributes.Select(n => n.LogicalName);
                    columns.AddRange(additionalColumns);
                }

                columnSet.Columns = columns.ToArray();
            }

            query.ColumnSet = columnSet;
            query.Distinct = false;
            query.EntityName = entityName;

            var pageInfo = new PagingInfo();
            pageInfo.Count = count;
            pageInfo.PageNumber = pageNumber;
            pageInfo.PagingCookie = cookie;

            query.PageInfo = pageInfo;
            query.NoLock = false;

            Organization_91_Online.EntityCollection response;
            if(connection.ConnectionType == ConnectionTypeEnum.Ad) {
                Organization_91_Online.OrganizationServiceClient adClient = (Organization_91_Online.OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint);
                response = adClient.RetrieveMultiple(query);
                
            } else {
                string requestXml03;
                XElement el03;
                using (MemoryStream memStm = new MemoryStream()) {
                    DataContractSerializer ser = new DataContractSerializer(typeof(QueryExpression));
                    ser.WriteObject(memStm, query);
                    memStm.Seek(0, SeekOrigin.Begin);

                    using (var streamReder = new StreamReader(memStm)){
                        requestXml03 = streamReder.ReadToEnd();
                        el03 = XElement.Parse(requestXml03);

                    }
                }

                XNamespace n = "http://schemas.microsoft.com/xrm/2011/Contracts/Services";
                XDocument doc = new XDocument(new XElement(n + "RetrieveMultiple"));

                el03.Name = "query";
                el03.Attributes().Where(e => e.Name == "xmlns").Remove();
                el03.Name = n + "query";
                
                el03.Add(new XAttribute(XNamespace.Xmlns + "b", "http://schemas.microsoft.com/xrm/2011/Contracts"));
                el03.Add(new XAttribute("{http://www.w3.org/2001/XMLSchema-instance}type", "b:QueryExpression"));
                
                doc.Root.Add(el03);

                string request04 = doc.FirstNode.ToString();

                request04 = "<s:Body>" + request04 + "</s:Body>";

                XDocument xDoc = ExecuteSoapRequestOrganizationEndpoint(request04, MethodNameEnum.RetrieveMultiple);
                if (xDoc == null) {
                    throw new Exception();
                }

                XElement collectionElement = xDoc.Descendants().First(l => l.Name.LocalName == "RetrieveMultipleResult");
                collectionElement.Name = "{http://schemas.microsoft.com/xrm/2011/Contracts}EntityCollection";

                using (XmlReader r = collectionElement.CreateReader()) {
                    DataContractSerializer ser = new DataContractSerializer(typeof(Organization_91_Online.EntityCollection));
                    response = (Organization_91_Online.EntityCollection)ser.ReadObject(r);
                }
            }

            FetchBatch batch = new FetchBatch();
            foreach (Organization_91_Online.Entity entity in response.Entities) {
                CrmRow row = new CrmRow();
                foreach(CrmAttribute attr in attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf))) {
                    CrmColumn col = new CrmColumn{CrmType = attr.CrmAttributeType, LogicalName = attr.LogicalName, Value = null};
                    var value = ((KeyValuePair<string, object>?)entity.Attributes.FirstOrDefault(n => n.Key == attr.LogicalName))?.Value;
                    row.Columns.Add(col);
                    if (value != null) {
                        switch (attr.CrmAttributeType) {
                            case AttributeTypeEnum.Picklist:
                            case AttributeTypeEnum.State:
                            case AttributeTypeEnum.Status:
                                col.Value = ((OptionSetValue)value).Value;
                            break;
                            case AttributeTypeEnum.MultiSelectPicklist:
                                var os = ((OptionSetValueCollection)value).Select(n => n.Value.ToString());
                                col.Value = string.Join(",", os);
                            break;
                            case AttributeTypeEnum.Lookup:
                            case AttributeTypeEnum.Customer:
                            case AttributeTypeEnum.Owner:
                                col.Value = ((EntityReference)value)?.Id;
                                break;
                            case AttributeTypeEnum.Money:
                                col.Value = ((Money)value)?.Value;
                                break;
                            case AttributeTypeEnum.PartyList:
                                var v = (EntityCollection)value;
                                if(v.Entities.Length > 0) {
                                    string json = $"[";
                                    foreach(Organization_91_Online.Entity e in v.Entities) {
                                        var partyId = (EntityReference)e.Attributes.First(n => n.Key == "partyid").Value;
                                        json += $"{{\"PartyId\":\"{partyId.Id}\",\"Name\":\"{partyId.Name}\", \"Type\":\"{partyId.LogicalName}\"}},";
                                    }
                                    json = json.Substring(0, json.Length - 1) + "]";
                                    col.Value = json;
                                }
                                break;
                            default:
                                col.Value = value;
                                break;
                        }
                    }
                }

                foreach(CrmAttribute attr in attributes.Where(n => !string.IsNullOrEmpty(n.AttributeOf))) {
                    var originalColumn = ((KeyValuePair<string, object>?)entity.Attributes.FirstOrDefault(n => n.Key == attr.AttributeOf))?.Value;
                    CrmColumn col = new CrmColumn { CrmType = attr.CrmAttributeType, LogicalName = attr.LogicalName, Value = null };
                    if (originalColumn != null && (originalColumn is OptionSetValue || originalColumn is OptionSetValueCollection || originalColumn is bool)) {
                        col.Value = ((KeyValuePair<string, string>?)entity.FormattedValues.FirstOrDefault(n => n.Key == attr.AttributeOf))?.Value;
                    }                    
                    else if(originalColumn != null && originalColumn is EntityReference er) {
                        if(attr.CrmAttributeType == AttributeTypeEnum.String) {
                            col.Value = er.Name;
                        }
                        else if(attr.CrmAttributeType == AttributeTypeEnum.EntityName) {
                            col.Value = er.LogicalName;
                        }
                    }
                    row.Columns.Add(col);
                }

                batch.Rows.Add(row);
            }

            batch.MoreRecords = response.MoreRecords;
            batch.PagingCookie = response.PagingCookie;

            return batch;
        }

        public FetchBatch ParseEntityCollection(EntityCollection entityCollection, List<CrmAttribute> attributes) {
            FetchBatch batch = new FetchBatch();
            foreach (Organization_91_Online.Entity entity in entityCollection.Entities) {
                CrmRow row = new CrmRow();
                foreach (CrmAttribute attr in attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf))) {
                    CrmColumn col = new CrmColumn { CrmType = attr.CrmAttributeType, LogicalName = attr.LogicalName, Value = null };
                    var value = ((KeyValuePair<string, object>?)entity.Attributes.FirstOrDefault(n => n.Key == attr.LogicalName))?.Value;
                    row.Columns.Add(col);
                    if (value != null && string.IsNullOrEmpty(attr.Alias)) {
                        switch (attr.CrmAttributeType) {
                            case AttributeTypeEnum.Picklist:
                            case AttributeTypeEnum.State:
                            case AttributeTypeEnum.Status:
                                col.Value = ((OptionSetValue)value).Value;
                                break;
                            case AttributeTypeEnum.MultiSelectPicklist:
                                var os = ((OptionSetValueCollection)value).Select(n => n.Value.ToString());
                                col.Value = string.Join(",", os);
                                break;
                            case AttributeTypeEnum.Lookup:
                            case AttributeTypeEnum.Customer:
                            case AttributeTypeEnum.Owner:
                                col.Value = ((EntityReference)value)?.Id;
                                break;
                            case AttributeTypeEnum.Money:
                                col.Value = ((Money)value)?.Value;
                                break;
                            case AttributeTypeEnum.PartyList:
                                var v1 = (EntityCollection)value;
                                if (v1.Entities.Length > 0) {
                                    string json = $"[";
                                    foreach (Organization_91_Online.Entity e in v1.Entities) {
                                        var partyId = (EntityReference)e.Attributes.First(n => n.Key == "partyid").Value;
                                        json += $"{{\"PartyId\":\"{partyId.Id}\",\"Name\":\"{partyId.Name}\", \"Type\":\"{partyId.LogicalName}\"}},";
                                    }
                                    json = json.Substring(0, json.Length - 1) + "]";
                                    col.Value = json;
                                }
                                break;
                            default:
                                col.Value = value;
                                break;
                        }
                    }
                    else if(value != null && !string.IsNullOrEmpty(attr.Alias)) {
                        var v = (AliasedValue)value;
                        switch(attr.CrmAttributeType) {
                            case AttributeTypeEnum.Picklist:
                            case AttributeTypeEnum.State:
                            case AttributeTypeEnum.Status:
                                col.Value = ((OptionSetValue)(v.Value)).Value;
                                break;
                            case AttributeTypeEnum.MultiSelectPicklist:
                                var os = ((OptionSetValueCollection)v.Value).Select(n => n.Value.ToString());
                                col.Value = string.Join(",", os);
                                break;
                            case AttributeTypeEnum.Lookup:
                            case AttributeTypeEnum.Customer:
                            case AttributeTypeEnum.Owner:
                                col.Value = ((EntityReference)v.Value)?.Id;
                                break;
                            case AttributeTypeEnum.Money:
                                col.Value = ((Money)v.Value)?.Value;
                                break;
                            case AttributeTypeEnum.PartyList:
                                var v1 = (EntityCollection)v.Value;
                                if (v1.Entities.Length > 0) {
                                    string json = $"[";
                                    foreach (Organization_91_Online.Entity e in v1.Entities)
                                    {
                                        var partyId = (EntityReference)e.Attributes.First(n => n.Key == "partyid").Value;
                                        json += $"{{\"PartyId\":\"{partyId.Id}\",\"Name\":\"{partyId.Name}\", \"Type\":\"{partyId.LogicalName}\"}},";
                                    }
                                    json = json.Substring(0, json.Length - 1) + "]";
                                    col.Value = json;
                                }
                                break;
                            default:
                                col.Value = v.Value;
                                break;
                        }
                    }
                }

                foreach (CrmAttribute attr in attributes.Where(n => !string.IsNullOrEmpty(n.AttributeOf))) {
                    string originalAttributeName = string.IsNullOrEmpty(attr.Alias) ? attr.AttributeOf : 
                        (attr.Alias + "_" + attr.NotAliasedName == attr.LogicalName ? attr.Alias : attr.Alias + "." + attr.AttributeOf);
                    var originalColumn = ((KeyValuePair<string, object>?)entity.Attributes.FirstOrDefault(n => n.Key == originalAttributeName))?.Value;
                    if(!string.IsNullOrEmpty(attr.Alias)) {
                        originalColumn = ((AliasedValue)originalColumn)?.Value;
                    }
                    CrmColumn col = new CrmColumn { CrmType = attr.CrmAttributeType, LogicalName = attr.LogicalName, Value = null };
                    if(originalColumn != null && originalColumn is EntityReference er) {
                        if (attr.CrmAttributeType == AttributeTypeEnum.String) {
                            col.Value = er.Name;
                        }
                        else if (attr.CrmAttributeType == AttributeTypeEnum.EntityName) {
                            col.Value = er.LogicalName;
                        }
                    }
                    else if(originalColumn != null) {
                        if(originalColumn is OptionSetValue || originalColumn is OptionSetValueCollection || originalColumn is bool) {
                            col.Value = ((KeyValuePair<string, string>?)entity.FormattedValues.FirstOrDefault(n => n.Key == attr.AttributeOf))?.Value;
                        }
                    }
                   
                    row.Columns.Add(col);
                }

                batch.Rows.Add(row);
            }

            batch.MoreRecords = entityCollection.MoreRecords;
            batch.PagingCookie = entityCollection.PagingCookie;

            return batch;
        }

        public override FetchBatch RetrieveAllByFetch(string fetchXml, string entitySetName, List<CrmAttribute> attributes, int pageNumber = 1, int count = 5000, string cookie = null) {
            XmlDocument fetchXmlDoc = new XmlDocument();
            fetchXmlDoc.LoadXml(fetchXml);
            XmlElement root = fetchXmlDoc.DocumentElement;
            root.SetAttribute("page", pageNumber.ToString());
            root.SetAttribute("count", count.ToString());
            if (cookie != null) {
                root.SetAttribute("paging-cookie", cookie);
            }
            fetchXml = fetchXmlDoc.InnerXml;

            FetchExpression query = new FetchExpression();
            query.Query = fetchXml;

            EntityCollection response;
            if(connection.ConnectionType == ConnectionTypeEnum.Ad) {
                OrganizationServiceClient adClient = (OrganizationServiceClient)transportLayer.SetWcfClient();
                response = adClient.RetrieveMultiple(query);
            } else {
                response = ExecuteSoapRequestOrganizationEndpoint<FetchExpression, EntityCollection>(query, typeof(EntityCollection), MethodNameEnum.RetrieveMultiple);
            }

            var x = ParseEntityCollection(response, attributes);

            return x;
        }

        public override List<Entity> RetrieveEntityList() {
            Organization_91_Online.EntityMetadata[] metadataArray;
            if (connection.ConnectionType == ConnectionTypeEnum.Ad) {
                Organization_91_Online.OrganizationServiceClient adClient = (Organization_91_Online.OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint);

                var request = new Organization_91_Online.RetrieveAllEntitiesRequest();
                request.RequestName = "RetrieveAllEntities";
                request.Parameters = new ParameterCollection();
                request.Parameters.Add(new KeyValuePair<string, object>("EntityFilters", EntityFilters.Privileges));
                request.Parameters.Add(new KeyValuePair<string, object>("RetrieveAsIfPublished", false));

                var response = (Organization_91_Online.RetrieveAllEntitiesResponse)adClient.Execute((Organization_91_Online.OrganizationRequest)request);
                metadataArray = (Organization_91_Online.EntityMetadata[])response.Results.First(n => n.Key == "EntityMetadata").Value;
            } else {
                string xml = @"<s:Body>
                            <Execute xmlns='http://schemas.microsoft.com/xrm/2011/Contracts/Services' xmlns:i='http://www.w3.org/2001/XMLSchema-instance'>
                                <request i:type='a:RetrieveAllEntitiesRequest' xmlns:a='http://schemas.microsoft.com/xrm/2011/Contracts'>
                                    <a:Parameters xmlns:b='http://schemas.datacontract.org/2004/07/System.Collections.Generic'>
                                        <a:KeyValuePairOfstringanyType>
                                            <b:key>EntityFilters</b:key>
                                            <b:value i:type='c:EntityFilters' xmlns:c='http://schemas.microsoft.com/xrm/2011/Metadata'>Entity</b:value>
                                        </a:KeyValuePairOfstringanyType>
                                        <a:KeyValuePairOfstringanyType>
                                            <b:key>RetrieveAsIfPublished</b:key>
                                            <b:value i:type='c:boolean' xmlns:c='http://www.w3.org/2001/XMLSchema'>true</b:value>
                                        </a:KeyValuePairOfstringanyType>
                                    </a:Parameters>
                                    <a:RequestId i:nil='true'/>
                                    <a:RequestName>RetrieveAllEntities</a:RequestName>
                                </request>
                            </Execute>
                          </s:Body>";

                XDocument xDoc = ExecuteSoapRequestOrganizationEndpoint(xml);
                if (xDoc == null) {
                    throw new Exception();
                }
                
                XElement resp = xDoc.Descendants("{http://schemas.microsoft.com/xrm/2011/Contracts/Services}ExecuteResult").First();
                resp.Name = "{http://schemas.microsoft.com/xrm/2011/Contracts}RetrieveAllEntitiesResponse";
                
                DataContractSerializer ser = new DataContractSerializer(typeof(Organization_91_Online.RetrieveAllEntitiesResponse));
                using (XmlReader r = resp.CreateReader()) {
                    var zz = (Organization_91_Online.RetrieveAllEntitiesResponse)ser.ReadObject(r);
                    metadataArray = (Organization_91_Online.EntityMetadata[])zz.Results.First(n => n.Key == "EntityMetadata").Value;
                }
            }

            List<Entity> entities = new List<Entity>();
            foreach(EntityMetadata entityMetadata in metadataArray) {
                Entity entity = new Entity();
                entity.LogicalName = entityMetadata.LogicalName;
                entity.IdName = entityMetadata.PrimaryIdAttribute;
                entity.IsIntersect = entityMetadata.IsIntersect ?? false;
                entity.ValidForAdvancedFind = entityMetadata.IsValidForAdvancedFind ?? false;
                entity.EntitySetName = entityMetadata.EntitySetName;
                entities.Add(entity);
                if(entityMetadata.LogicalName == "uom") {
                    var x = 1;
                }
            }

            return entities;
        }

        public override List<CrmAttribute> RetrieveAttributesList(EntityModel model, Entity entity) {
            var request = new Organization_91_Online.RetrieveEntityRequest();
            request.RequestName = "RetrieveEntity";
            request.Parameters = new ParameterCollection();
            KeyValuePair<string, object> entityFilters = new KeyValuePair<string, object>("EntityFilters", (EntityFilters.Attributes | EntityFilters.Relationships));
            request.Parameters.Add(entityFilters);
            request.Parameters.Add(new KeyValuePair<string, object>("RetrieveAsIfPublished", false));
            request.Parameters.Add(new KeyValuePair<string, object>("LogicalName", entity.LogicalName));
            request.Parameters.Add(new KeyValuePair<string, object>("MetadataId", new Guid("00000000-0000-0000-0000-000000000000")));

            Organization_91_Online.RetrieveEntityResponse response;
            if (connection.ConnectionType == ConnectionTypeEnum.Ad) {
                Organization_91_Online.OrganizationServiceClient adClient = (Organization_91_Online.OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint);

                response = (Organization_91_Online.RetrieveEntityResponse)adClient.Execute((Organization_91_Online.OrganizationRequest)request);
            } else {
                string xml = "";
                xml += "  <s:Body>";
                xml += "    <Execute xmlns=\"http://schemas.microsoft.com/xrm/2011/Contracts/Services\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">";
                xml += "      <request i:type=\"a:RetrieveEntityRequest\" xmlns:a=\"http://schemas.microsoft.com/xrm/2011/Contracts\">";
                xml += "        <a:Parameters xmlns:b=\"http://schemas.datacontract.org/2004/07/System.Collections.Generic\">";
                xml += "          <a:KeyValuePairOfstringanyType>";
                xml += "            <b:key>EntityFilters</b:key>";
                xml += "            <b:value i:type=\"c:EntityFilters\" xmlns:c=\"http://schemas.microsoft.com/xrm/2011/Metadata\">Attributes Relationships</b:value>";
                xml += "          </a:KeyValuePairOfstringanyType>";
                xml += "          <a:KeyValuePairOfstringanyType>";
                xml += "            <b:key>MetadataId</b:key>";
                xml += "            <b:value i:type=\"c:guid\" xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/\">00000000-0000-0000-0000-000000000000</b:value>";
                xml += "          </a:KeyValuePairOfstringanyType>";
                xml += "          <a:KeyValuePairOfstringanyType>";
                xml += "            <b:key>RetrieveAsIfPublished</b:key>";
                xml += "            <b:value i:type=\"c:boolean\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">false</b:value>";
                xml += "          </a:KeyValuePairOfstringanyType>";
                xml += "          <a:KeyValuePairOfstringanyType>";
                xml += "            <b:key>LogicalName</b:key>";
                xml += $"            <b:value i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">{entity.LogicalName}</b:value>";
                xml += "          </a:KeyValuePairOfstringanyType>";
                xml += "        </a:Parameters>";
                xml += "        <a:RequestId i:nil=\"true\" />";
                xml += "        <a:RequestName>RetrieveEntity</a:RequestName>";
                xml += "      </request>";
                xml += "    </Execute>";
                xml += "  </s:Body>";

                XDocument xDoc = ExecuteSoapRequestOrganizationEndpoint(xml);
                if (xDoc == null) {
                    throw new Exception();
                }
                XElement resp = xDoc.Descendants("{http://schemas.microsoft.com/xrm/2011/Contracts/Services}ExecuteResult").First();
                resp.Name = "{http://schemas.microsoft.com/xrm/2011/Contracts}RetrieveEntityResponse";

                using (XmlReader r = resp.CreateReader()) {
                    DataContractSerializer ser = new DataContractSerializer(typeof(Organization_91_Online.RetrieveEntityResponse));
                    response = (Organization_91_Online.RetrieveEntityResponse)ser.ReadObject(r);
                }
            }

            Organization_91_Online.EntityMetadata entityMetadata = (Organization_91_Online.EntityMetadata)response.Results.First(n => n.Key == "EntityMetadata").Value;
            var attributes = new List<Organization_91_Online.AttributeMetadata>(entityMetadata.Attributes);
            var manyToOne = entityMetadata.ManyToOneRelationships.ToList();
            var primaryIdAttribute = entityMetadata.PrimaryIdAttribute;
            if(entity != null) {
                entity.IdName = primaryIdAttribute;
            }
            attributes = attributes.OrderBy(n => n.LogicalName).Where(t => t.IsValidForRead == true || t.IsValidForCreate == true || t.IsValidForUpdate == true).ToList();

            List<CrmAttribute> crmAttributes = new List<CrmAttribute>();
            foreach (AttributeMetadata metadata in attributes) {
                CrmAttribute a = new CrmAttribute();
                a.LogicalName = metadata.LogicalName;
                a.DisplayName = metadata.DisplayName?.UserLocalizedLabel?.Label;
                a.AttributeOf = metadata.AttributeOf;
                if(metadata.GetType() == typeof(StringAttributeMetadata)) {
                    string yomi = ((StringAttributeMetadata)metadata).YomiOf;
                    if(!string.IsNullOrEmpty(a.AttributeOf) && !string.IsNullOrEmpty(yomi)) {
                        continue;
                    }
                }
                AttributeTypeCode type = metadata.AttributeType.Value;
                a.CrmAttributeType = (AttributeTypeEnum)((int)type);
                if (a.CrmAttributeType == AttributeTypeEnum.Virtual && metadata.AttributeTypeName?.Value?.ToLower() == "multiselectpicklisttype") {
                    a.CrmAttributeType = AttributeTypeEnum.MultiSelectPicklist;
                }

                a.DisplayForRead = metadata.IsValidForRead ?? false;
                if (metadata.IsPrimaryId == true || a.CrmAttributeType == AttributeTypeEnum.State || a.CrmAttributeType == AttributeTypeEnum.Status) {
                    a.DisplayForCreate = a.DisplayForUpdate = true;
                } else {
                    a.DisplayForCreate = string.IsNullOrEmpty(metadata.AttributeOf) && (metadata.IsValidForCreate ?? false);
                    a.DisplayForUpdate = string.IsNullOrEmpty(metadata.AttributeOf) && (metadata.IsValidForUpdate ?? false);
                }

                a.Length = 0;
                a.Precision = 0;
                a.Scale = 0;
                a.Entity = entity.LogicalName;

                switch (a.CrmAttributeType) {
                    case AttributeTypeEnum.Picklist:
                        var p1 = (PicklistAttributeMetadata)metadata;
                        var p2 = (OptionSetMetadata)p1.OptionSet;
                        var p3 = (OptionMetadata[])p2.Options;
                        a.OptionSetValues = new Dictionary<int, List<string>>();
                        foreach(OptionMetadata md in p3) {
                            var value = md.Value;
                            var x4 = md.Label.LocalizedLabels.Select(n => n.Label.ToLower().Trim()).ToList();
                            a.OptionSetValues.Add(value.Value, new List<string>(x4));
                        }
                        break;
                    case AttributeTypeEnum.State:
                        var x1 = (StateAttributeMetadata)metadata;
                        var x2 = (OptionSetMetadata)x1.OptionSet;
                        var x3 = (OptionMetadata[])x2.Options;
                        a.OptionSetValues = new Dictionary<int, List<string>>();
                        foreach (OptionMetadata md in x3) {
                            var value = md.Value;
                            var x4 = md.Label.LocalizedLabels.Select(n => n.Label.ToLower().Trim()).ToList();
                            a.OptionSetValues.Add(value.Value, new List<string>(x4));
                        }
                        break;
                    case AttributeTypeEnum.Status:
                        var y1 = (StatusAttributeMetadata)metadata;
                        var y2 = (OptionSetMetadata)y1.OptionSet;
                        var y3 = (OptionMetadata[])y2.Options;
                        a.OptionSetValues = new Dictionary<int, List<string>>();
                        foreach (OptionMetadata md in y3) {
                            var value = md.Value;
                            var x4 = md.Label.LocalizedLabels.Select(n => n.Label.ToLower().Trim()).ToList();
                            a.OptionSetValues.Add(value.Value, new List<string>(x4));
                        }
                        break;
                    case AttributeTypeEnum.DateTime:
                        a.Scale = 7;
                        break;
                    case AttributeTypeEnum.Decimal:
                        var td = (Organization_91_Online.DecimalAttributeMetadata)metadata;
                        a.Scale = td.Precision ?? 0;
                        a.Precision = 38;
                        break;
                    case AttributeTypeEnum.Lookup:
                    case AttributeTypeEnum.Customer:
                    case AttributeTypeEnum.Owner:
                        var tl = (Organization_91_Online.LookupAttributeMetadata)metadata;
                        a.PossibleLookups = new List<Lookup>();
                        var custom = tl.IsCustomAttribute;
                        if(custom == true) {
                            SetLookups(model, a, manyToOne);
                        } else {
                            var targets = tl.Targets;
                            foreach(string target in targets) {
                                Lookup l = new Lookup {
                                    LookupOdataName = a.LogicalName,
                                    TargetEntityName = target,
                                    TargetEntitySetName = model.EntityList.FirstOrDefault(n => n.LogicalName == target)?.EntitySetName
                                };
                                if(l.TargetEntitySetName != null) {
                                    a.PossibleLookups.Add(l);
                                }

                                if(a.LogicalName == "objectid") {
                                    var xx = a;
                                }
                            }
                        }
                        break;
                    case AttributeTypeEnum.Money:
                        var tm = (Organization_91_Online.MoneyAttributeMetadata)metadata;
                        a.Scale = tm.Precision ?? 0;
                        a.Precision = 38;
                        break;
                    case AttributeTypeEnum.String:
                        var ts = (Organization_91_Online.StringAttributeMetadata)metadata;
                        a.Length = ts.MaxLength ?? 1;
                        break;
                    case AttributeTypeEnum.EntityName:
                        a.Length = 64;
                        break;
                    case AttributeTypeEnum.Memo:
                        var tme = (Organization_91_Online.MemoAttributeMetadata)metadata;
                        a.Length = tme.MaxLength ?? 1;
                        break;
                    case AttributeTypeEnum.MultiSelectPicklist:
                        a.Length = 255;
                        break;
                    case AttributeTypeEnum.Virtual:
                        a.Length = 255;
                        if(attributes.FirstOrDefault(n => n.LogicalName == a.AttributeOf)?.AttributeTypeName?.Value.ToLower() == "multiselectpicklisttype") {
                            a.Length = 4000;
                        }
                        break;
                }
                crmAttributes.Add(a);
            }

            if(entity != null) {
                var keysMetadata = (Organization_91_Online.EntityKeyMetadata[])entityMetadata.Keys;
                if(keysMetadata != null) {
                    entity.AlternateKeys = new List<AlternateKey>();
                    foreach(EntityKeyMetadata metadata in keysMetadata) {
                        AlternateKey key = new AlternateKey { EntityLogicalName = entity.LogicalName, EntitySetName = entity.EntitySetName, KeyColumns = new List<CrmAttribute>() };
                        key.LogicalName = metadata.LogicalName;
                        foreach(string attribute in metadata.KeyAttributes) {
                            key.KeyColumns.Add(new CrmAttribute {
                                LogicalName = attribute,
                                CrmAttributeType = crmAttributes.First(n => n.LogicalName == attribute).CrmAttributeType
                            });
                        }
                        entity.AlternateKeys.Add(key);
                    }
                }
            }

            if(entityMetadata.IsIntersect == true) {
                var manyToManyAll = entityMetadata.ManyToManyRelationships;
                var manyToMany = manyToManyAll.First(n => n.IntersectEntityName == entity.LogicalName);
                CrmAttribute a1 = new CrmAttribute();
                a1.LogicalName = manyToMany.Entity1IntersectAttribute;
                a1.CrmAttributeType = AttributeTypeEnum.Lookup;
                a1.Entity = entity.LogicalName;
                a1.DisplayForRead = a1.DisplayForCreate = true;
                a1.DisplayForUpdate = false;
                Lookup l1 = new Lookup();
                l1.LookupOdataName = l1.TargetEntityName = manyToMany.Entity1LogicalName;
                l1.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l1.TargetEntityName).EntitySetName;
                a1.PossibleLookups.Add(l1);

                CrmAttribute a2 = new CrmAttribute();
                a2.LogicalName = manyToMany.Entity2IntersectAttribute;
                a2.CrmAttributeType = AttributeTypeEnum.Lookup;
                a2.Entity = entity.LogicalName;
                a2.DisplayForRead = a2.DisplayForCreate = true;
                a2.DisplayForUpdate = false;
                Lookup l2 = new Lookup();
                l2.LookupOdataName = l2.TargetEntityName = manyToMany.Entity2LogicalName;
                l2.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l2.TargetEntityName).EntitySetName;
                a2.PossibleLookups.Add(l2);

                var forRemove = crmAttributes.Where(n => n.LogicalName == a1.LogicalName || n.LogicalName == a2.LogicalName).ToList();
                crmAttributes.Remove(forRemove[0]);
                crmAttributes.Remove(forRemove[1]);
                crmAttributes.AddRange(new []{a1, a2});

                var primaryKey = crmAttributes.First(n => n.CrmAttributeType == AttributeTypeEnum.Uniqueidentifier);
                primaryKey.DisplayForCreate = primaryKey.DisplayForUpdate = false;
                if(entity != null) {
                    entity.IntersectRelationship = new CrmComponents.Model.Relationship {
                        Entity1AttributeName = manyToMany.Entity1IntersectAttribute,
                        Entity1EntityName = manyToMany.Entity1LogicalName,
                        Entity1EntitySetName = l1.TargetEntitySetName,
                        Entity2AttributeName = manyToMany.Entity2IntersectAttribute,
                        Entity2EntityName = manyToMany.Entity2LogicalName,
                        Entity2EntitySetName = l2.TargetEntitySetName,
                        Name = manyToMany.Entity1NavigationPropertyName
                    };
                }
            }

            return crmAttributes;
        }

        private void SetLookups(EntityModel model, CrmAttribute attribute, List<OneToManyRelationshipMetadata> manyToOneList) {
            var targetRelationship = manyToOneList.Where(n => n.ReferencingAttribute == attribute.LogicalName);
            foreach(OneToManyRelationshipMetadata metadata in targetRelationship) {
                Lookup l1 = new Lookup();
                l1.TargetEntityName = metadata.ReferencedEntity;
                l1.LookupOdataName = metadata.ReferencingEntityNavigationPropertyName;
                l1.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l1.TargetEntityName).EntitySetName;
                attribute.PossibleLookups.Add(l1);
            }
        }
        public override bool CheckOrganizationUri() {
            bool result = true;
            if(connection.ConnectionType == ConnectionTypeEnum.Ad || connection.ConnectionType == ConnectionTypeEnum.Adfs) {
                Uri organizationUri = connection.Organization.OrganizationService;
                string baseDiscoveriUri = connection.DiscoveryService.AbsoluteUri.Replace(connection.DiscoveryService.AbsolutePath, "");
                string baseOrganizationUri = connection.Organization.OrganizationService.AbsoluteUri.Replace(connection.Organization.OrganizationService.AbsolutePath, "");

                if(baseOrganizationUri != baseDiscoveriUri) {
                    if(!WhoAmI()) {
                        connection.Organization.OrganizationService = new Uri(baseDiscoveriUri + organizationUri.AbsolutePath);
                        if(!WhoAmI()) {
                            connection.Organization.OrganizationService = organizationUri;
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        public override bool WhoAmI(int timeoutSec = 30) {
            Organization_91_Online.WhoAmIRequest request = new WhoAmIRequest();
            request.RequestName = "WhoAmI";
            request.Parameters = new ParameterCollection();
            
            Organization_91_Online.WhoAmIResponse response;

            try {
                if (connection.ConnectionType == ConnectionTypeEnum.Ad) {
                    Organization_91_Online.OrganizationServiceClient adClient = (Organization_91_Online.OrganizationServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.OrganizationEndpoint, timeoutSec: timeoutSec);
                    response = (Organization_91_Online.WhoAmIResponse)adClient.Execute((Organization_91_Online.OrganizationRequest)request);
                    var x = 1;
                } else {
                    response = (WhoAmIResponse)ExecuteSoapRequestOrganizationEndpoint<WhoAmIRequest, WhoAmIResponse>(request, typeof(WhoAmIResponse), MethodNameEnum.Execute);
                }

                Guid? userId = response.Results.FirstOrDefault(n => n.Key == "UserId").Value as Guid?;
                return userId != null;
            }
            catch (Exception ex) {
                return false;
            }
        }

        public K ExecuteSoapRequestOrganizationEndpoint<T, K>(T request, Type responseType, MethodNameEnum method = MethodNameEnum.Execute, int timeoutSec = 30) {
            StringBuilder sb = new StringBuilder();
            K response = default(K);
            string requestName = method == MethodNameEnum.Execute
                ? (request as Organization_91_Online.OrganizationRequest)?.RequestName
                : request.GetType().Name;
            string requestNs = null;

            XDocument doc = null;

            using(MemoryStream ms = new MemoryStream()) {
                DataContractSerializer ser = new DataContractSerializer(typeof(T));
                ser.WriteObject(ms, request);
                ms.Seek(0, SeekOrigin.Begin);

                using(var streamReader = new StreamReader(ms)) {
                    string xmlString = streamReader.ReadToEnd();
                    sb.Append(xmlString);
                }
            }

            if(method == MethodNameEnum.RetrieveMultiple) {
                XElement el = XElement.Parse(sb.ToString());

                XNamespace n = "http://schemas.microsoft.com/xrm/2011/Contracts/Services";
                el.Name = "query";
                el.Attributes().Where(t => t.Name == "xmlns").Remove();
                el.Name = n + "query";
                el.Add(new XAttribute(XNamespace.Xmlns + "b", "http://schemas.microsoft.com/xrm/2011/Contracts"));
                el.Add(new XAttribute("{http://www.w3.org/2001/XMLSchema-instance}type", $"b:{requestName}"));

                doc = new XDocument(new XElement(n + "RetrieveMultiple"));
                doc.Root.Add(el);
            }
            else if(method == MethodNameEnum.Execute && typeof(T) != typeof(ExecuteMultipleRequest)) {
                XElement el = XElement.Parse(sb.ToString());

                XNamespace n = "http://schemas.microsoft.com/xrm/2011/Contracts/Services";
                XNamespace m = "http://schemas.microsoft.com/xrm/2011/Contracts";
                
                doc = new XDocument(new XElement(n + "Execute"));
                doc.Root.Add(el);

                requestNs = el.Name.NamespaceName;
                el.Name = "request";
                el.Attributes().Where(t => t.Name == "xmlns").Remove();
                el.Name = n + "request";

                el.Add(new XAttribute(XNamespace.Xmlns + "b", m.NamespaceName));
                el.Add(new XAttribute(XNamespace.Xmlns + "c", requestNs));
                el.Add(new XAttribute("{http://www.w3.org/2001/XMLSchema-instance}type",
                    $"c:{requestName}Request"));

                foreach (XElement xEl in doc.Root.DescendantsAndSelf()) {
                    if (xEl.Name.NamespaceName == m.NamespaceName) {
                        xEl.Name = xEl.Name.LocalName;
                        xEl.Attributes().Where(t => t.Name == "xmlns").Remove();
                        xEl.Name = m + xEl.Name.LocalName;
                    }
                }
            }
            else if(method == MethodNameEnum.Execute && typeof(T) == typeof(ExecuteMultipleRequest)) {
                var ss = sb.ToString();
                XElement el = XElement.Parse(sb.ToString());

                XNamespace n = "http://schemas.microsoft.com/xrm/2011/Contracts/Services";
                XNamespace m = "http://schemas.microsoft.com/xrm/2011/Contracts";

                doc = new XDocument(new XElement(n + "Execute"));
                doc.Root.Add(el);

                requestNs = el.Name.NamespaceName;
                el.Name = "request";
                el.Attributes().Where(t => t.Name == "xmlns").Remove();
                el.Name = n + "request";

                el.Add(new XAttribute(XNamespace.Xmlns + "b", m.NamespaceName));
                el.Add(new XAttribute("{http://www.w3.org/2001/XMLSchema-instance}type",
                    $"b:{requestName}Request"));
                foreach (XElement xEl in doc.Root.DescendantsAndSelf()) {
                    if (xEl.Name.LocalName == "OrganizationRequest") {
                        xEl.Attributes().Remove();
                    }
                }
            }

            string req = $@"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
                                {transportLayer.Header(EndpointTypeEnum.OrganizationEndpoint, method)}
                                <s:Body>{doc?.FirstNode.ToString()}</s:Body>
                            </s:Envelope>";

            if (connection.Organization.OrganizationVersion >= 9) {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(connection.Organization.OrganizationService);
            httpRequest.Timeout = timeoutSec * 1000;
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] bytesToWrite = encoding.GetBytes(req);
            httpRequest.Method = "POST";
            httpRequest.ContentLength = bytesToWrite.Length;
            httpRequest.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = httpRequest.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            string test02 = UTF8Encoding.UTF8.GetString(bytesToWrite);

            XDocument xDoc = null;
            using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse()) {
                using (Stream dataStream = httpResponse.GetResponseStream()) {
                    if (dataStream != null) {
                        using (StreamReader reader = new StreamReader(dataStream)) {
                            xDoc = XDocument.Load(reader);
                        }
                    }
                }
            }

            if(xDoc == null) {
                throw new Exception();
            }

            if(method == MethodNameEnum.RetrieveMultiple) {
                XElement collectionElement = xDoc.Descendants().First(l => l.Name.LocalName == "RetrieveMultipleResult");
                collectionElement.Name = "{http://schemas.microsoft.com/xrm/2011/Contracts}EntityCollection";

                using (XmlReader r = collectionElement.CreateReader()) {
                    DataContractSerializer ser = new DataContractSerializer(responseType);
                    var resp = ser.ReadObject(r);
                    response = (K)resp;
                }
            }
            else if(method == MethodNameEnum.Execute) {
                XNamespace p = requestNs;
                XElement resp = xDoc.Descendants("{http://schemas.microsoft.com/xrm/2011/Contracts/Services}ExecuteResult").First();
                resp.Name = p + $"{requestName}Response";

                using(XmlReader r = resp.CreateReader()) {
                    DataContractSerializer ser =  new DataContractSerializer(responseType);
                    response = (K)ser.ReadObject(r);
                }
            }

            return response;
        }

        public XDocument ExecuteSoapRequestOrganizationEndpoint(string requestBody, MethodNameEnum method = MethodNameEnum.Execute) {
            string xml = "";
            xml += "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">";
            xml += transportLayer.Header(EndpointTypeEnum.OrganizationEndpoint, method);
            xml += requestBody;
            xml += "</s:Envelope>";

            if (connection.Organization.OrganizationVersion >= 9) {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(connection.Organization.OrganizationService);
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] bytesToWrite = encoding.GetBytes(xml);
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XDocument x = null;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream != null) {
                        using (StreamReader reader = new StreamReader(dataStream)) {
                            x = XDocument.Load(reader);
                        }
                    }
                }
            }

            return x;
        }

        public override List<Organization> RetrieveOrganizations(int timeoutSec = 30) {
            Discovery_92_Online.OrganizationDetailCollection collection;
            if(connection.ConnectionType == ConnectionTypeEnum.Ad) {
                var adClient = (Discovery_92_Online.DiscoveryServiceClient)transportLayer.SetWcfClient(EndpointTypeEnum.DiscoveryEndpoint, timeoutSec:timeoutSec);
                var organizationsRequest = new Discovery_92_Online.RetrieveOrganizationsRequest();
                collection = ((Discovery_92_Online.RetrieveOrganizationsResponse)adClient.Execute(organizationsRequest)).Details;
            } else {
                collection = transportLayer.ExecuteDiscoveryOnline(timeoutSec);
            }

            List<Organization> organizations = new List<Organization>();
            foreach(Discovery_92_Online.OrganizationDetail detail in collection) {
                Organization org = new Organization();
                org.FriendlyName = detail.FriendlyName;
                org.OrganizationId = detail.OrganizationId;
                org.OrganizationService = new Uri(detail.Endpoints.First(n => n.Key == EndpointType.OrganizationService).Value);
                string[] version = detail.OrganizationVersion.Split('.');
                org.OrganizationVersion = Int32.Parse(version[0]) + Int32.Parse(version?[1] ?? "0")*0.1m;
                org.UniqueName = detail.UniqueName;
                org.UrlName = detail.UrlName;
                organizations.Add(org);
            }

            return organizations;
        }
    }
}
