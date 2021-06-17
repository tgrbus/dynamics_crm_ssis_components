using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Security.Cryptography;

namespace CrmComponents
{
    [DtsPipelineComponent(DisplayName = "Hash Component", ComponentType = ComponentType.Transform, IconResource = "CrmComponents.Resources.Icon1.ico")]
    public class HashComponent : PipelineComponent {
        private int[] inputColumns;
        private int outputColumn;
        public override void ProvideComponentProperties() {
            IDTSInput100 input = ComponentMetaData.InputCollection.New();
            input.Name = "Input";

            IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
            output.Name = "Output";
            output.SynchronousInputID = input.ID;

            IDTSOutputColumn100 hash = output.OutputColumnCollection.New();
            hash.Name = "Hash";
            hash.SetDataTypeProperties(DataType.DT_WSTR, 66, 0, 0, 0);
        }

        public override void PreExecute() {
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            IDTSOutput100 output = ComponentMetaData.OutputCollection[0];

            inputColumns = new int[input.InputColumnCollection.Count];

            for(int i = 0; i < input.InputColumnCollection.Count; i++) {
                IDTSInputColumn100 inputColumn = input.InputColumnCollection[i];
                inputColumns[i] = BufferManager.FindColumnByLineageID(input.Buffer, inputColumn.LineageID);
            }

            IDTSOutputColumn100 _outputColumn = output.OutputColumnCollection[0];
            outputColumn = BufferManager.FindColumnByLineageID(input.Buffer, _outputColumn.LineageID);
        }

        public override void ProcessInput(int InputID, PipelineBuffer buffer) {
            SHA256 sha = new SHA256CryptoServiceProvider();
            while (buffer.NextRow()) {
                string hashSource = "";
                for(int i = 0; i < inputColumns.Length; i++) {
                    if(!buffer.IsNull(inputColumns[i])) {
                        hashSource += buffer[inputColumns[i]].ToString();
                    } 
                }

                byte[] hasBytes = sha.ComputeHash(System.Text.UnicodeEncoding.Unicode.GetBytes(hashSource));
                System.Text.StringBuilder sb = new StringBuilder();
                for(int i = 0; i < hasBytes.Length; i++) {
                    sb.Append(hasBytes[i].ToString("X2"));
                }
                buffer[outputColumn] = "0x" + sb.ToString();
            }
        }

        public override DTSValidationStatus Validate() {
            return ComponentMetaData.InputCollection[0].InputColumnCollection.Count < 1 ? DTSValidationStatus.VS_ISCORRUPT : base.Validate();
        }
    }
}
