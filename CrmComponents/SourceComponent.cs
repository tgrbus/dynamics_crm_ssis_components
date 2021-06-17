using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CrmComponents.Model;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CrmComponents.Soap;
using DTSBreakpointHitTest = Microsoft.SqlServer.Dts.Runtime.DTSBreakpointHitTest;

namespace CrmComponents
{
    [DtsPipelineComponent(DisplayName = "CRM Source", ComponentType = ComponentType.SourceAdapter, IconResource = "CrmComponents.Resources.Icon1.ico",
        UITypeName = "CrmComponents.SourceComponentInterface, CrmComponents, Version=1.2014.12.0, Culture=neutral, PublicKeyToken=81423fe9ba0539ea")]
    public class SourceComponent : PipelineComponent {
        private DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(EntityModel));
        private EntityModel model;
        private List<ColumnInfo> columnInformation;
        private string sourceType;
        private string fetchXml;
        private string connectionString = null;
        private int batchSize = 2000;
        private int maxRowsCount = 0;
        List<SsisVariable> vars = new List<SsisVariable>();

        private struct ColumnInfo {
            public int BufferColumnIndex;
            public string ColumnName;
        }

        private const string MyLogEntryName = "My Custom Component Log Entry";
        private const string MyLogEntryDescription = "Log entry from My Custom Component ";

        public override void RegisterLogEntries() {
            this.LogEntryInfos.Add(MyLogEntryName, MyLogEntryDescription, Microsoft.SqlServer.Dts.Runtime.Wrapper.DTSLogEntryFrequency.DTSLEF_CONSISTENT);
        }

        public override void AcquireConnections(object transaction) {
            if (ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager != null) {
                ConnectionManager cm = DtsConvert.GetWrapper(ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager);
                if (cm.InnerObject is CrmConnection) {
                    connectionString = cm.ConnectionString;
                }
            }
        }

        public override void ReleaseConnections() {
            connectionString = null;
        }

        public override void PrimeOutput(int outputs, int[] outputIDs, PipelineBuffer[] buffers) {
            string completeFetch = "";
            if (sourceType.ToLower() != "entity") {
                FetchXmlHelpers fetchHelpers = new FetchXmlHelpers();
                IDTSVariables100 variables = null;
                List<string> variableNames = fetchHelpers.GetVariablesNames(fetchXml);

                if(variableNames.Count > 0) {
                    vars = new List<SsisVariable>();
                    VariableDispenser.Reset();
                    foreach (string name in variableNames) {
                        VariableDispenser.LockForRead(name);
                    }
                    
                    VariableDispenser.GetVariables(out variables);
                    foreach (IDTSVariable100 dtsVariable100 in variables) {
                        var x2 = dtsVariable100.QualifiedName;
                        var x1 = dtsVariable100.DataType;
                        var x3 = dtsVariable100.Value;
                        vars.Add(new SsisVariable { DataType = x1, Name = x2, Value = x3 });
                        
                    }
                    variables.Unlock();
                    completeFetch = fetchHelpers.SetVariables(fetchXml, vars);
                }
                else {
                    completeFetch = fetchXml;
                }
            }
            

            IDTSOutput100 output100 = ComponentMetaData.OutputCollection[0];
            PipelineBuffer buffer = buffers[0];

            Connection conn = new Connection(connectionString);
            CrmCommands commands = Connection.CrmCommandsFactory(conn);
            
            bool moreRecords = true;
            int pageNumber = 1;
            string cookie = null;

            int recordCount = 0;
            List<CrmRow> rows = new List<CrmRow>();
            int count = batchSize;
            if(maxRowsCount > 0 && maxRowsCount < batchSize) {
                count = maxRowsCount;
            }
            int notSelected = model.Attributes.Where(n => n.DisplayForRead && string.IsNullOrEmpty(n.AttributeOf)).Count(k => !k.Selected);

            var selectedAttributes = model.Attributes.Where(n => n.Selected).ToList();
            var attributesOff = model.Attributes.Where(n => n.Selected && !string.IsNullOrEmpty(n.AttributeOf));
            var additionalAttributes = model.Attributes.Where(n => attributesOff.Any(l => l.AttributeOf == n.LogicalName)).ToList();
            additionalAttributes = additionalAttributes.Where(n => !selectedAttributes.Exists(l => l.LogicalName == n.LogicalName)).ToList();

            while (moreRecords) {
                byte[] additionalData = null;
                string message = $"MaxRowsCount:{maxRowsCount}, BatchSize:{batchSize}, RecordCount:{recordCount}";
                this.ComponentMetaData.PostLogMessage(MyLogEntryName, this.ComponentMetaData.Name, message, DateTime.Now, DateTime.Now, 0, ref additionalData);
                FetchBatch batch;

                batch = sourceType.ToLower() == "entity" ? 
                    commands.RetrieveAllRecords(model.SelectedEntity.LogicalName, model.SelectedEntity.EntitySetName, selectedAttributes, additionalAttributes,pageNumber++, cookie, count, notSelected == 0) : 
                    commands.RetrieveAllByFetch(completeFetch, model.SelectedEntity.EntitySetName, model.Attributes.Where(n => n.Selected).ToList(), pageNumber++, count, cookie);

                recordCount += batch.Rows.Count;
                if(maxRowsCount == 0) {
                    moreRecords = batch.MoreRecords;
                } else {
                    moreRecords = batch.MoreRecords && recordCount < maxRowsCount;
                }

                cookie = batch.PagingCookie;
                rows.AddRange(batch.Rows);
                if(rows.Count > 0) {
                    foreach (CrmRow row in rows){
                        buffer.AddRow();
                        for (int x = 0; x < columnInformation.Count; x++) {
                            ColumnInfo ci = columnInformation[x]; 
                            CrmColumn col = row.Columns.First(n => n.LogicalName == ci.ColumnName);
                            if (col.Value == null) {
                                buffer.SetNull(ci.BufferColumnIndex);
                            }
                            else {
                                buffer[ci.BufferColumnIndex] = col.Value;
                            }
                        }
                    }
                    rows = new List<CrmRow>();
                }
            }
            buffer.SetEndOfRowset();
        }

        public override void PreExecute() {
            columnInformation = new List<ColumnInfo>();
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];

            foreach(IDTSOutputColumn100 col in output.OutputColumnCollection) {
                ColumnInfo ci = new ColumnInfo();
                ci.BufferColumnIndex = BufferManager.FindColumnByLineageID(output.Buffer, col.LineageID);
                ci.ColumnName = col.Name;
                columnInformation.Add(ci);
            }

            

            foreach(IDTSCustomProperty100 property in ComponentMetaData.CustomPropertyCollection) {
                if(property.Name == "Source") {
                    sourceType = property.Value as string;
                }
                if(property.Name == "FetchXML") {
                    fetchXml = property.Value as string;
                }
                if(property.Name == "Batch Size" && property.Value != null) {
                    batchSize = (int)property.Value;
                }
                if(property.Name == "Max Rows Count" && property.Value != null) {
                    maxRowsCount = (int)property.Value;
                }
            }
        }

        public override DTSValidationStatus Validate() {
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];
            if(output.OutputColumnCollection.Count == 0) {
                return DTSValidationStatus.VS_ISBROKEN;
            }

            DTSValidationStatus baseStatus = base.Validate();
            return baseStatus;
        }

        public override void ProvideComponentProperties() {
            base.RemoveAllInputsOutputsAndCustomProperties();

            IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
            output.Name = "Output";

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
            property03.Name = "Source";//Entity, FetchXML
            property03.Description = "Source Type";
            property03.Value = "Entity";

            IDTSCustomProperty100 property04 = ComponentMetaData.CustomPropertyCollection.New();
            property04.Name = "FetchXML";
            property04.Description = "Fetch XML";
            property04.Value = "";

            IDTSCustomProperty100 property05 = ComponentMetaData.CustomPropertyCollection.New();
            property05.Name = "Batch Size";
            property05.Description = "Batch Size";
            property05.Value = 2000;

            IDTSCustomProperty100 property06 = ComponentMetaData.CustomPropertyCollection.New();
            property06.Name = "Max Rows Count";
            property06.Description = "Max Rows Count";
            property06.Value = 0;
        }

        public override void Initialize() {
            base.Initialize();
            model = null;
            foreach (IDTSCustomProperty100 property100 in ComponentMetaData.CustomPropertyCollection) {
                if (property100.Name == "Serialized State" && !string.IsNullOrEmpty(property100.Value.ToString())) {
                    string p = (string) property100.Value;
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(p))) {
                        model = (EntityModel) serializer.ReadObject(ms);
                    }
                    break;
                }
            }

            if (model != null) {
                foreach (IDTSCustomProperty100 property100 in ComponentMetaData.CustomPropertyCollection) {
                    if (property100.Name == "State") {
                        property100.Value = model;
                        break;
                    }
                }
            }
        }
    }
}
