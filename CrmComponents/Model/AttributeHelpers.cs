using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmComponents.Helpers;
using CrmComponents.Helpers.Enums;

namespace CrmComponents.Model
{
    public class AttributeHelpers {
        public static string AddColumnValueToUniqueString(string curentValue, string attributeName, string nextValue) {
            return curentValue + $"{attributeName}:{nextValue.ToLower()}-";
        }
        public static string ColumnToString(CrmColumn col) {
            string stringValue = null;
            var crmType = col.CrmType;
            if(col.Value == null) {
                stringValue = "";
            } else {
                switch(crmType) {
                    case AttributeTypeEnum.BigInt:
                        if(col.Value.GetType() != typeof(long)) {
                            col.Value = long.Parse(col.Value.ToString());
                        }

                        stringValue = col.Value?.ToString();
                        break;
                    case AttributeTypeEnum.Boolean:
                        if(col.Value.GetType() != typeof(bool)) {
                            col.Value = col.Value is int ? Convert.ToBoolean(col.Value) : bool.Parse(col.Value.ToString());
                        }

                        stringValue = ((bool?)col.Value == null) ? null : ((bool)col.Value ? "true" : "false");
                        break;
                    case AttributeTypeEnum.Customer:
                    case AttributeTypeEnum.Lookup:
                    case AttributeTypeEnum.Owner:
                    case AttributeTypeEnum.Uniqueidentifier:
                        if(col.Value.GetType() != typeof(Guid?)) {
                            col.Value = Guid.Parse(col.Value.ToString());
                        }

                        stringValue = col.Value?.ToString();
                        break;
                    case AttributeTypeEnum.DateTime:
                        if(col.Value.GetType() != typeof(DateTime?)) {
                            col.Value = DateTime.Parse(col.Value.ToString());
                        }

                        stringValue = ((DateTime?)col.Value)?.ToString("yyyy-MM-dd HH:mm.ss");
                        break;
                    case AttributeTypeEnum.Decimal:
                    case AttributeTypeEnum.Money:
                        if(col.Value.GetType() != typeof(decimal?)) {
                            col.Value = decimal.Parse(col.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }

                        stringValue = ((decimal?)col.Value)?.ToString(NumberFormat.NFI.nfi);
                        break;
                    case AttributeTypeEnum.Double:
                        if(col.Value.GetType() != typeof(double?)) {
                            col.Value = double.Parse(col.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                        }

                        stringValue = ((double?)col.Value)?.ToString(NumberFormat.NFI.nfi);
                        break;
                    case AttributeTypeEnum.Picklist:
                    case AttributeTypeEnum.State:
                    case AttributeTypeEnum.Status:
                        int? optionValue = AttributeHelpers.SetOptionsetValue(col);
                        if(optionValue == null) {
                            throw new Exception("Can't find optionset value for " + col.Value.ToString());
                        }

                        stringValue = optionValue.Value.ToString();
                        break;
                    case AttributeTypeEnum.Integer:
                        if(col.Value.GetType() != typeof(int?)) {
                            col.Value = int.Parse(col.Value.ToString());
                        }

                        stringValue = ((int?)col.Value)?.ToString();
                        break;
                    case AttributeTypeEnum.Memo:
                    case AttributeTypeEnum.String:
                    case AttributeTypeEnum.MultiSelectPicklist:
                        if(col.Value.GetType() != typeof(string)) {
                            col.Value = col.Value.ToString();
                        }

                        //stringValue = col.Value != null ? col.Value.ToString().Trim() : "";
                        stringValue = col.Value?.ToString()?.Trim();
                        break;
                }
            }

            return stringValue;
        }

        public static int? SetOptionsetValue(CrmColumn col) {
            object value = col.Value;
            col.OriginalValue = value;
            int? result = null;
            int optionValue;

            if(value.GetType() != typeof(int?)) {
                if(int.TryParse(value.ToString(), out optionValue)) {
                    result = optionValue;
                } else {
                    string label = value.ToString().ToLower().Trim();
                    foreach (KeyValuePair<int, List<string>> option in col.CrmAttribute.OptionSetValues) {
                        if (option.Value.Contains(label)) {
                            result = option.Key;
                            break;
                        }
                    }
                }
            } else {
                result = (int?)col.Value;
            }

            return result;
        }
    }
}
