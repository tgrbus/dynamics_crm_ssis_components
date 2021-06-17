using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.Serialization;
using CrmComponents.Helpers.Enums;


namespace CrmComponents.Model
{
    [DataContract]
    public class CrmAttribute {

        public CrmSsisTypeMapping Mapping;
        public CrmAttribute() {
            Mapping = new CrmSsisTypeMapping();
            //MatchingLookupColumn = null;
            PossibleLookups = new List<Lookup>();
            //LookupTarget = null;
        }

        [DataMember]
        public string AttributeOf { get; set; }

        [DataMember]
        public bool Selected { get; set; }
        [DataMember]
        public string Entity { get; set; }
        [DataMember]
        public string LogicalName { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
        [IgnoreDataMember]
        public string NotAliasedName {
            get {
                if(string.IsNullOrEmpty(this.LogicalName)) {
                    return null;
                }
                else if(string.IsNullOrEmpty(this.Alias)) {
                    return this.LogicalName;
                }
                else if(this.Alias == this.LogicalName) {
                    return "";
                }
                else {
                    //remove alias prvo
                    
                    string naname = this.LogicalName.Remove(0, this.Alias.Length).Substring(1);
                    return naname;
                }
            } }

        [DataMember]
        public string Alias { get; set; }
        [DataMember]
        public bool DisplayForCreate { get; set; }
        [DataMember]
        public bool DisplayForUpdate { get; set; }
        [DataMember]
        public bool DisplayForRead { get; set; }
        [DataMember]
        public AttributeTypeEnum CrmAttributeType { get; set; }

        private DataType? _ssisDataType;

        public DataType SsisDataType {
            get {
                if (_ssisDataType != null) {
                    return _ssisDataType.Value;
                }
                else {
                    DataType temp;
                    if(Mapping == null) {
                        Mapping = new CrmSsisTypeMapping();
                    }
                    temp = Mapping.ReturnSsisType(CrmAttributeType);
                    _ssisDataType = temp;
                    return temp;
                }
            }
            set => _ssisDataType = value;
        }
        [DataMember]
        public int Length { get; set; } //length of text/string field
        [DataMember]
        public int Precision { get; set; }
        [DataMember]
        public int Scale { get; set; }

        //lookup & optionset matching attributes
        [DataMember]
        public ComplexMappingEnum ComplexMapping { get; set; }
        [DataMember]
        public AlternateKey LookupAlternateKey { get; set; }
        [DataMember]
        public Lookup LookupTarget { get; set; } //entity lookup target
        [DataMember]
        public List<Lookup> PossibleLookups { get; set; }

        [DataMember]
        public List<CrmAttribute> MatchingLookupAttributes { get; set; } = new List<CrmAttribute>();

        [DataMember]
        public string MatchedMultiple { get; set; }
        [DataMember]
        public string MatchNotFound { get; set; }

        [DataMember]
        public string MatchingDefaultValue { get; set; } = null;
        [DataMember]
        public string SsisInput { get; set; }
        [DataMember]
        public bool NotEntityAttribute { get; set; } = false;

        [DataMember]
        public Dictionary<int, List<string>> OptionSetValues;

        public bool IsForMatching() {
            if(NotEntityAttribute) {
                return false;
            }
            if(CrmAttributeType == AttributeTypeEnum.Lookup ||
               CrmAttributeType == AttributeTypeEnum.Customer ||
               CrmAttributeType == AttributeTypeEnum.Owner) {
                return true;
            } else {
                return false;
            }
        }

        public CrmAttribute ShallowCopy() {
            return (CrmAttribute)this.MemberwiseClone();
        }
    }
}
