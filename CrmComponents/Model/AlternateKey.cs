using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CrmComponents.Model
{
    [DataContract]
    public class AlternateKey
    {
        [DataMember]
        public string LogicalName { get; set; }
        [DataMember]
        public string EntityLogicalName { get; set; }
        [DataMember]
        public string EntitySetName { get; set; }
        [DataMember]
        public List<CrmAttribute> KeyColumns { get; set; }

        /*
         * Alternate key possible data types
         *
         * Decimal Number
         * Whole Number
         * Single line of text
         * Date Time
         * Lookup: _tg_lookup_value=2ba17064-1ae7-e611-80f4-e0071b661f01 _[LogicalName]_value=[guid]
         * Picklist
         *
         * https://crm04.crm4.dynamics.com/api/data/v9.1/accounts(tg_dateandtime=2020-07-17T06:00:00Z,_tg_lookup_value=2ba17064-1ae7-e611-80f4-e0071b661f01,tg_optionset=100000000,tg_decimalnumber=63.5,accountnumber='ABC28UU7') HTTP/1.1
         */
    }
}
