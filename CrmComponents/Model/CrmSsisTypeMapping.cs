using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace CrmComponents.Model
{
    public class CrmSsisTypeMapping
    {
        public Type ReturnCType(AttributeTypeEnum crmType) {
            return typeof(int);
        }

        public DataType ReturnSsisType(AttributeTypeEnum crmType) {
            DataType temp;
            switch (crmType)
            {
                case AttributeTypeEnum.BigInt:
                    temp = DataType.DT_I8;
                    break;
                case AttributeTypeEnum.Boolean:
                    temp = DataType.DT_BOOL;
                    break;
                case AttributeTypeEnum.Customer:
                    temp = DataType.DT_GUID;
                    break;
                case AttributeTypeEnum.DateTime:
                    temp = DataType.DT_DBTIMESTAMP2;
                    break;
                case AttributeTypeEnum.Decimal:
                    temp = DataType.DT_NUMERIC;
                    break;
                case AttributeTypeEnum.Double:
                    temp = DataType.DT_R8;
                    break;
                case AttributeTypeEnum.Integer:
                    temp = DataType.DT_I4;
                    break;
                case AttributeTypeEnum.Lookup:
                    temp = DataType.DT_GUID;
                    break;
                case AttributeTypeEnum.Money:
                    temp = DataType.DT_NUMERIC;
                    break;
                case AttributeTypeEnum.Owner:
                    temp = DataType.DT_GUID;
                    break;
                case AttributeTypeEnum.PartyList:
                    temp = DataType.DT_NTEXT;
                    break;
                case AttributeTypeEnum.Picklist:
                    temp = DataType.DT_I4;
                    break;
                case AttributeTypeEnum.State:
                    temp = DataType.DT_I4;
                    break;
                case AttributeTypeEnum.Status:
                    temp = DataType.DT_I4;
                    break;
                case AttributeTypeEnum.String:
                    temp = DataType.DT_WSTR;
                    break;
                case AttributeTypeEnum.Memo:
                    temp = DataType.DT_NTEXT;
                    break;
                case AttributeTypeEnum.Uniqueidentifier:
                    temp = DataType.DT_GUID;
                    break;
                case AttributeTypeEnum.MultiSelectPicklist:
                    temp = DataType.DT_WSTR;
                    break;
                case AttributeTypeEnum.Virtual:
                    temp = DataType.DT_WSTR;
                    break;
                case AttributeTypeEnum.EntityName:
                    temp = DataType.DT_WSTR;
                    break;
                default:
                    temp = DataType.DT_NULL;
                    break;
            }

            return temp;
        }

        
    }
}
