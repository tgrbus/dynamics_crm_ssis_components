using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace CrmComponents.Model
{
    [DataContract]
    public class SsisInput
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public DataType SsisType { get; set; }
        [DataMember]
        public string CrmColumnName { get; set; }
    }
}
