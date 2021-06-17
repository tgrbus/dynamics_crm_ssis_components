using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Parameters = CrmComponents.Model.Parameters;

namespace CrmComponents {
    [DtsPipelineComponent(DisplayName = "CRM Destination", ComponentType = ComponentType.DestinationAdapter, 
        IconResource = "CrmComponents.Resources.Icon1.ico", CurrentVersion = 4,
        UITypeName = "CrmComponents.DestinationComponentInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea")]
    public class DestinationComponent : PipelineComponent {
        private DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EntityModel));
        private EntityModel model;
        private OperationTypeEnum operation;
        private ComplexMappingEnum matchingCriteria;
        private string alternateKey;
        private MatchingEnum multipleMatch;
        private MatchingEnum matchNotFound;
        private string connectionString = null;
        private int batchSize;
        private int noOfThreads;
        private ErrorHandlingEnum errorHandling;

        private Col[] inputColumns;
        private Col[] outputColumns;
        private Col[] errorColumns;
        
        private const string MyLogEntryName = "My Custom Destination Log Entry";
        private const string MyLogEntryDescription = "Log entry from My Custom Destination Component ";

        public class Col {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CrmName { get; set; }
            public DataType? StreamDataType { get; set; }
            public int CodePage { get; set; }
        }

        public override void RegisterLogEntries() {
            this.LogEntryInfos.Add(MyLogEntryName, MyLogEntryDescription, Microsoft.SqlServer.Dts.Runtime.Wrapper.DTSLogEntryFrequency.DTSLEF_CONSISTENT);
        }

        public override void AcquireConnections(object transaction) {
            if(ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager != null) {
                ConnectionManager cm = DtsConvert.GetWrapper(ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager);
                if(cm.InnerObject is CrmConnection) {
                    connectionString = cm.ConnectionString;
                }
            }
        }

        public override void ReleaseConnections() {
            connectionString = null;
        }

        public override void ProvideComponentProperties() {
            ComponentMetaData.Version = 4;
            ComponentMetaData.UsesDispositions = true;

            IDTSInput100 input = ComponentMetaData.InputCollection.New();
            input.Name = "Input";
            input.ErrorRowDisposition = DTSRowDisposition.RD_NotUsed;
            input.HasSideEffects = true;

            IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
            output.Name = "Output";
            output.SynchronousInputID = input.ID;
            output.ExclusionGroup = 1;

            IDTSOutput100 errorOutput = ComponentMetaData.OutputCollection.New();
            errorOutput.IsErrorOut = true;
            errorOutput.Name = "ErrorOutput";
            errorOutput.SynchronousInputID = input.ID;
            errorOutput.ExclusionGroup = 1;

            IDTSOutputColumn100 c1 = output.OutputColumnCollection.New();
            c1.Name = Parameters.GuidOutputName;
            c1.SetDataTypeProperties(DataType.DT_GUID, 0, 0, 0, 0);

            IDTSOutputColumn100 c2 = output.OutputColumnCollection.New();
            c2.Name = Parameters.RecordCreatedOutputName;
            c2.SetDataTypeProperties(DataType.DT_BOOL, 0, 0, 0, 0);

            IDTSOutputColumn100 c3 = errorOutput.OutputColumnCollection.New();
            c3.Name = Parameters.ErrorMessageOutputName;
            c3.SetDataTypeProperties(DataType.DT_WSTR, 4000, 0, 0, 0);

            IDTSRuntimeConnection100 conn = ComponentMetaData.RuntimeConnectionCollection.New();
            conn.Name = "nesto";

            IDTSCustomProperty100 property01 = ComponentMetaData.CustomPropertyCollection.New();
            property01.Name = "State";
            property01.Description = "State";
            property01.Value = new EntityModel();

            IDTSCustomProperty100 property02 = ComponentMetaData.CustomPropertyCollection.New();
            property02.Name = "Serialized State";
            property02.Description = "Serialized State";
            property02.Value = "";

            IDTSCustomProperty100 property03 = ComponentMetaData.CustomPropertyCollection.New();
            property03.Name = "Operation";
            property03.Description = "Operation";
            property03.Value = "Update";

            IDTSCustomProperty100 property04 = ComponentMetaData.CustomPropertyCollection.New();
            property04.Name = "Matching Criteria";
            property04.Description = "Matching Criteria";
            property04.Value = "";

            IDTSCustomProperty100 property05 = ComponentMetaData.CustomPropertyCollection.New();
            property05.Name = "Alternate Key";
            property05.Description = "Alternate Key";
            property05.Value = "";

            IDTSCustomProperty100 property06 = ComponentMetaData.CustomPropertyCollection.New();
            property06.Name = "Multiple Matches";
            property06.Description = "Multiple Matches";
            property06.Value = "";

            IDTSCustomProperty100 property07 = ComponentMetaData.CustomPropertyCollection.New();
            property07.Name = "Match Not Found";
            property07.Description = "Match Not Found";
            property07.Value = "";

            IDTSCustomProperty100 property08 = ComponentMetaData.CustomPropertyCollection.New();
            property08.Name = "Batch Size";
            property08.Description = "Batch Size";
            property08.Value = 50;

            IDTSCustomProperty100 property09 = ComponentMetaData.CustomPropertyCollection.New();
            property09.Name = "No of Threads";
            property09.Description = "No of Threads";
            property09.Value = 1;

            IDTSCustomProperty100 property10 = ComponentMetaData.CustomPropertyCollection.New();
            property10.Name = "Error Handling";
            property10.Description = "Error Handling";
            property10.Value = "Fail on Error";

        }
        public override void Initialize() {
            base.Initialize();

            model = null;
            foreach(IDTSCustomProperty100 property in ComponentMetaData.CustomPropertyCollection) {
                if(property.Name == "Serialized State" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    string p = (string)property.Value;
                    using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(p))) {
                        model = (EntityModel)serializer.ReadObject(ms);
                    }
                }
                if(property.Name == "Operation" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    string prop = (string)property.Value;
                    operation = (OperationTypeEnum)System.Enum.Parse(typeof(OperationTypeEnum), prop);
                }

                if(property.Name == "Matching Criteria" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    string prop = (string)property.Value;
                    matchingCriteria = (ComplexMappingEnum)Dictionaries.GetInstance().ComplexMappingNames.FirstOrDefault(n => n.Value == prop).Key;
                }

                if(property.Name == "Alternate Key" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    alternateKey = (string)property.Value;
                }

                if(property.Name == "Multiple Matches" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    multipleMatch = (MatchingEnum)Dictionaries.GetInstance().MatchingNames.First(n => n.Value == (string)property.Value).Key;
                }

                if(property.Name == "Match Not Found" && !string.IsNullOrEmpty(property.Value.ToString())) {
                    matchNotFound = (MatchingEnum)Dictionaries.GetInstance().MatchingNames.FirstOrDefault(n => n.Value == (string)property.Value).Key;
                }

                if(property.Name == "Batch Size" && property.Value != null) {
                    batchSize = (int)property.Value;
                }

                if(property.Name == "No of Threads" && property.Value != null) {
                    noOfThreads = (int)property.Value;
                }

                if(property.Name == "Error Handling" && property.Value != null) {
                    errorHandling = (ErrorHandlingEnum)Dictionaries.GetInstance().ErrorHandlingNames.FirstOrDefault(n => n.Value == (string)property.Value).Key;
                }
            }

            if(model != null) {
                foreach(IDTSCustomProperty100 property in ComponentMetaData.CustomPropertyCollection) {
                    if(property.Name == "State") {
                        property.Value = model;
                        break;
                    }
                }
            }
        }

        public override void PerformUpgrade(int pipelineVersion) {
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            if (output.SynchronousInputID == 0) {
                output.OutputColumnCollection.RemoveAll();
                output.SynchronousInputID = input.ID;

                IDTSOutputColumn100 c1 = output.OutputColumnCollection.New();
                c1.Name = Parameters.GuidOutputName;
                c1.SetDataTypeProperties(DataType.DT_GUID, 0, 0, 0, 0);

                IDTSOutputColumn100 c2 = output.OutputColumnCollection.New();
                c2.Name = Parameters.RecordCreatedOutputName;
                c2.SetDataTypeProperties(DataType.DT_BOOL, 0, 0, 0, 0);
            }

            if(!input.HasSideEffects) {
                input.HasSideEffects = true;
            }
        }

        public override void PreExecute() {
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];
            IDTSOutput100 errorOutput = ComponentMetaData.OutputCollection[1];

            inputColumns = new Col[input.InputColumnCollection.Count];
            outputColumns = new Col[output.OutputColumnCollection.Count];
            errorColumns = new Col[errorOutput.OutputColumnCollection.Count];

            for(int i = 0; i < input.InputColumnCollection.Count; i++) {
                inputColumns[i] = new Col();
                IDTSInputColumn100 inputColumn = input.InputColumnCollection[i];
                inputColumns[i].Id = BufferManager.FindColumnByLineageID(input.Buffer, inputColumn.LineageID);
                inputColumns[i].Name = inputColumn.Name;
                inputColumns[i].CrmName = model.SsisInputs.FirstOrDefault(n => n.Name == inputColumn.Name)?.CrmColumnName;
                if(inputColumn.DataType == DataType.DT_TEXT || inputColumn.DataType == DataType.DT_NTEXT) {
                    inputColumns[i].StreamDataType = inputColumn.DataType;
                    inputColumns[i].CodePage = inputColumn.CodePage;
                }
            }

            for (int i = 0; i < output.OutputColumnCollection.Count; i++) {
                IDTSOutputColumn100 outputColumn = output.OutputColumnCollection[i];
                outputColumns[i] = new Col();
                outputColumns[i].Id = BufferManager.FindColumnByLineageID(input.Buffer, outputColumn.LineageID);
                outputColumns[i].Name = outputColumn.Name;
            }

            if(errorHandling == ErrorHandlingEnum.Redirect) {
                for(int i = 0; i < errorOutput.OutputColumnCollection.Count; i++) {
                    IDTSOutputColumn100 errorColumn = errorOutput.OutputColumnCollection[i];
                    errorColumns[i] = new Col();
                    errorColumns[i].Id = BufferManager.FindColumnByLineageID(input.Buffer, errorColumn.LineageID);
                    errorColumns[i].Name = errorColumn.Name;
                }
            }
        }

        private object GetInputValue(PipelineBuffer buffer, Col c) {
            object v = null;
            if (c.StreamDataType == DataType.DT_NTEXT) {
                v = System.Text.Encoding.Unicode.GetString(buffer.GetBlobData(c.Id, 0, (int)buffer.GetBlobLength(c.Id)));
            }
            else if(c.StreamDataType == DataType.DT_TEXT) {
                v = System.Text.Encoding.GetEncoding(c.CodePage).GetString(buffer.GetBlobData(c.Id, 0, (int)buffer.GetBlobLength(c.Id)));
            }
            return v;
        }

        public override void ProcessInput(int InputID, PipelineBuffer buffer) {
            int counter = 1;
            int counterA = 1;
            int defaultOutputId = ComponentMetaData.OutputCollection[0].ID;
            int errorOutputId = ComponentMetaData.OutputCollection[1].ID;
            List<OrganizationRequestModel> requests = new List<OrganizationRequestModel>();
            List<AttributeMatchingRequestModel> idMatchingRequests = new List<AttributeMatchingRequestModel>();
            List<AttributeMatchingRequestModel> attributeLookupRequests = new List<AttributeMatchingRequestModel>();
            CrmCommands commands = Connection.CrmCommandsFactory(connectionString);
            AttributeHelpers attributeHelpers = new AttributeHelpers();
            while (buffer.NextRow()) {
                bool idColumnsNull = false;
                OrganizationRequestModel req = new OrganizationRequestModel {
                    Entity = model.SelectedEntity,
                    Index = counter,
                    BufferRowPosition = buffer.CurrentRow
                };

                req.Row = new CrmRow { Columns = new List<CrmColumn>() };
                bool IdMatchingSet = false;
                for(int i = 0; i < inputColumns.Length; i++) {
                    string debug = "beginning 399";
                    CrmColumn c = new CrmColumn();
                    try {
                        object columnData = inputColumns[i].StreamDataType == null ? buffer[inputColumns[i].Id] : GetInputValue(buffer, inputColumns[i]);

                        if(inputColumns[i].CrmName != null) {
                            c.LogicalName = inputColumns[i].CrmName;
                            c.SssisName = inputColumns[i].Name;
                            CrmAttribute attribute = model.Attributes.First(n => n.LogicalName == inputColumns[i].CrmName);
                            c.CrmAttribute = attribute;
                            c.CrmType = attribute.CrmAttributeType;
                            c.ComplexMapping = attribute.ComplexMapping;
                            if(attribute.PossibleLookups.Count > 0) {
                                c.LookupTarget = attribute.LookupTarget ?? attribute.PossibleLookups[0];
                            }

                            if(attribute.ComplexMapping == ComplexMappingEnum.AlternateKey) {
                                //attribute level complex mapping alternate key
                                c.AlternateKeys = new List<CrmColumn>();
                                c.LookupTarget = attribute.PossibleLookups.First(n => n.TargetEntityName == attribute.LookupAlternateKey.EntityLogicalName); // attribute.LookupAlternateKey.EntityLogicalName;
                                foreach(CrmAttribute crmAttribute in attribute.LookupAlternateKey.KeyColumns) {
                                    CrmColumn cA = new CrmColumn();
                                    cA.LogicalName = crmAttribute.LogicalName;
                                    cA.CrmType = crmAttribute.CrmAttributeType;
                                    Col c1 = inputColumns.First(n => n.Name == crmAttribute.SsisInput);
                                    cA.Value = c1.StreamDataType == null ? buffer[c1.Id] : GetInputValue(buffer, c1);
                                    c.AlternateKeys.Add(cA);
                                }

                                if(c.AlternateKeys.All(n => n.Value == null)) {
                                    c.ComplexMapping = ComplexMappingEnum.PrimaryKey;
                                    c.Value = null;
                                }
                            } else if(attribute.ComplexMapping == ComplexMappingEnum.Manual) {
                                //attribute level complex mapping manual columns
                                AttributeMatchingRequestModel reqA = new AttributeMatchingRequestModel() {
                                    Entity = model.EntityList.First(n => n.LogicalName == c.LookupTarget.TargetEntityName), // c.LookupTarget,
                                    Index = counterA++,
                                    AttributeMatchDefaultValue = attribute.MatchingDefaultValue,
                                    AttributeMatchMultiple = (MatchingEnum)Enum.Parse(typeof(MatchingEnum), attribute.MatchedMultiple),
                                    AttributeMatchNotFound = (MatchingEnum)Enum.Parse(typeof(MatchingEnum), attribute.MatchNotFound),
                                };
                                reqA.Row = new CrmRow { Columns = new List<CrmColumn>() };
                                foreach(CrmAttribute lookupAttribute in attribute.MatchingLookupAttributes) {
                                    CrmColumn cA = new CrmColumn {
                                        LogicalName = lookupAttribute.LogicalName,
                                        CrmType = lookupAttribute.CrmAttributeType,
                                        CrmAttribute = lookupAttribute
                                    };
                                    Col cACol = inputColumns.First(n => n.Name == lookupAttribute.SsisInput);
                                    cA.Value = cACol.StreamDataType == null ? buffer[cACol.Id] : GetInputValue(buffer, cACol);
                                    if (cA.Value is string value) {
                                        if (string.IsNullOrEmpty(value)) {
                                            cA.Value = null;
                                        }
                                    }
                                    string colValue = AttributeHelpers.ColumnToString(cA);
                                    reqA.AttributeMatchUniqueString = AttributeHelpers.AddColumnValueToUniqueString(reqA.AttributeMatchUniqueString, cA.LogicalName, colValue);
                                    reqA.Row.Columns.Add(cA);
                                }

                                reqA.Row.Columns = reqA.Row.Columns.OrderBy(n => n.LogicalName).ToList();

                                reqA.OriginalColumnForAttributeLookup = c;//GUID will be set by reference 
                                attributeLookupRequests.Add(reqA);
                            } else {
                                c.Value = buffer.IsNull(inputColumns[i].Id) ? null : columnData;
                            }

                            debug = "id section 459";
                            //id setting section
                            if(!IdMatchingSet && matchingCriteria == ComplexMappingEnum.Manual) {
                                AttributeMatchingRequestModel reqId = new AttributeMatchingRequestModel() {
                                    Entity = model.SelectedEntity,
                                    Index = counter,
                                    AttributeMatchDefaultValue = null,
                                    AttributeMatchMultiple = multipleMatch,
                                    AttributeMatchNotFound = matchNotFound
                                };
                                reqId.Row = new CrmRow { Columns = new List<CrmColumn>() };
                                List<CrmAttribute> matchingAttributes = model.Attributes.Where(n => model.MatchingColumns.Any(k => k == n.LogicalName)).ToList();
                                foreach(CrmAttribute attr in matchingAttributes) {
                                    CrmColumn cA = new CrmColumn {
                                        LogicalName = attr.LogicalName,
                                        CrmType = attr.CrmAttributeType,
                                        CrmAttribute = attr
                                    };
                                    Col cACol = inputColumns.First(n => n.Name == attr.SsisInput);
                                    cA.Value = cACol.StreamDataType == null ? buffer[cACol.Id] : GetInputValue(buffer, cACol);
                                    if(cA.Value is string value) {
                                        if(string.IsNullOrEmpty(value)) {
                                            cA.Value = null;
                                        }
                                    }
                                    string colValue = AttributeHelpers.ColumnToString(cA);
                                    reqId.AttributeMatchUniqueString = AttributeHelpers.AddColumnValueToUniqueString(reqId.AttributeMatchUniqueString, cA.LogicalName, colValue);
                                    reqId.Row.Columns.Add(cA);
                                }

                                if(reqId.Row.Columns.All(n => n.Value == null)) {
                                    idColumnsNull = true;
                                }
                                reqId.Row.Columns = reqId.Row.Columns.OrderBy(n => n.LogicalName).ToList();
                                reqId.OriginalColumnForAttributeLookup = new CrmColumn();
                                idMatchingRequests.Add(reqId);
                            } else if(matchingCriteria == ComplexMappingEnum.PrimaryKey
                                      && model.SelectedEntity.IdName == attribute.LogicalName
                                      && (operation == OperationTypeEnum.Update || operation == OperationTypeEnum.Upsert || operation == OperationTypeEnum.Delete)) {
                                if(c.Value.GetType() != typeof(Guid)) {
                                    c.Value = new Guid(c.Value.ToString());
                                }

                                req.PrimaryId = (Guid?)c.Value;
                            } else if(matchingCriteria == ComplexMappingEnum.AlternateKey
                                      && (operation == OperationTypeEnum.Update || operation == OperationTypeEnum.Upsert || operation == OperationTypeEnum.Delete)
                                      && model.SelectedEntity.AlternateKeys.First(n => n.LogicalName == alternateKey).KeyColumns.Exists(n => n.LogicalName == attribute.LogicalName)
                            ) {
                                debug = "inside alt key if 498";
                                req.AlternateKeys.Add(c); }

                            req.Row.Columns.Add(c);
                        }
                    } catch(Exception ex) {
                        throw new Exception($"{debug}/{c.CrmAttribute.LogicalName}/{ex.Message}/{ex.StackTrace}");
                    }
                }

                if(matchingCriteria == ComplexMappingEnum.AlternateKey && model.SelectedEntity.AlternateKeys.FirstOrDefault(n => n.LogicalName == alternateKey).KeyColumns.Count != req.AlternateKeys.Count) {
                    throw new Exception("Not all alternate keys have a value");
                }

                if(!idColumnsNull) {
                    requests.Add(req);
                } else if(idColumnsNull && errorHandling == ErrorHandlingEnum.Fail) {
                    var last = idMatchingRequests[idMatchingRequests.Count - 1];
                    string errorMessage = $"Null or empty values for finding id column: {string.Join(",", last.Row.Columns.Select(k => k.LogicalName))}";
                    throw new Exception(errorMessage);
                }

                counter++;
            }

            try {
                string message = $"id requests: {idMatchingRequests.Count}";
                if(attributeLookupRequests.Count > 0) {
                    MultithreadAttributeManual mam = new MultithreadAttributeManual(connectionString, attributeLookupRequests, batchSize, errorHandling);
                    mam.Execute(noOfThreads);
                }

                if(idMatchingRequests.Count > 0) {
                    MultithreadAttributeManual mam2 = new MultithreadAttributeManual(connectionString, idMatchingRequests, batchSize, errorHandling);
                    mam2.Execute(noOfThreads);
                    if(matchingCriteria == ComplexMappingEnum.Manual) {
                        foreach(OrganizationRequestModel request in requests) {
                            var founded = idMatchingRequests.First(n => n.Index == request.Index).OriginalColumnForAttributeLookup;
                            request.PrimaryId = (Guid?)founded.Value;
                            message += request.PrimaryId + "/";
                        }
                    }
                }
            } catch(Exception ex) {
                throw new Exception($"multithread/{ex.Message}/{ex.StackTrace}");
            }

            MultiThreadObject mto = new MultiThreadObject(connectionString, operation, matchingCriteria, requests, batchSize, errorHandling, ComponentMetaData);
            if (operation == OperationTypeEnum.Merge) {
                mto.ExecuteMerge(noOfThreads, model.EntityList.First(n => n.LogicalName == model.SelectedEntity.LogicalName));
            }
            else if (model.SelectedEntity.IsIntersect) {
                mto.ExecuteIntersect(noOfThreads, model.SelectedEntity.IntersectRelationship);
            }
            else {
                mto.Execute(noOfThreads);
            }


            int guidColumnId = outputColumns.First(n => n.Name == Parameters.GuidOutputName).Id;
            int isNewColumnId = outputColumns.First(n => n.Name == Parameters.RecordCreatedOutputName).Id;
            
            foreach (OrganizationRequestModel req1 in requests) {
                buffer.CurrentRow = req1.BufferRowPosition;
                if (req1.Response != null && req1.Response.Success) {
                    if (req1.Response?.Id != null) {
                        buffer[guidColumnId] = req1.Response.Id.Value;
                    }
                    if (req1.Response?.RecordCreated != null) {
                        buffer[isNewColumnId] = req1.Response.RecordCreated.Value;
                    }
                    buffer.DirectRow(defaultOutputId);
                }
                else if (errorHandling == ErrorHandlingEnum.Fail) {
                    throw new Exception(req1.Response?.ErrorMessage);
                }
                else if(errorHandling == ErrorHandlingEnum.Redirect) {
                    int forErrorColumnId = errorColumns.First(n => n.Name == Parameters.ErrorMessageOutputName).Id;
                    string errorMessage = req1.Response?.ErrorMessage ?? "";
                    buffer[forErrorColumnId] = errorMessage.Length < 4000 ? errorMessage : errorMessage.Substring(0, 4000);
                    buffer.DirectErrorRow(errorOutputId, -2, inputColumns.First().Id);
                }
            }
        }
    }
}
