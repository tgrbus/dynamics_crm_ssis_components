using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CrmComponents.Helpers;

namespace CrmComponents.Model
{
    public class SsisVariable
    {
        public int DataType { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public override string ToString() {
            string sValue = null;
            if(Value is bool) {
                bool v3 = (bool?)Value ?? false;
                sValue = v3 ? "1" : "0";
            } else if(Value is char) {
                char v4 = (char?)Value ?? ' ';
                sValue = v4.ToString();
            }
            else if(Value is DateTime) {
                DateTime v16 = (DateTime?)Value ?? DateTime.MinValue;
                if (v16.Kind == DateTimeKind.Local) {
                    v16 = DateTime.SpecifyKind(v16, DateTimeKind.Local);
                    DateTimeOffset offset = v16;
                    string off = offset.Offset.Hours.ToString("00");
                    sValue = v16.ToString($"yyyy-MM-ddTHH:mm:ss+{off}");
                }
                else {
                    sValue = v16.ToString($"yyyy-MM-ddTHH:mm:ssZ");
                }
            }
            else if(Value is decimal value) {
                sValue = value.ToString("#.#", NumberFormat.NFI.nfi) ?? "";
            }
            else if(Value is double d) {
                sValue = d.ToString("#.#", NumberFormat.NFI.nfi) ?? "";
            }
            else if(Value is float f) {
                sValue = f.ToString("#.#", NumberFormat.NFI.nfi) ?? "";
            } else {
                sValue = Value?.ToString()??"";
            }
            return sValue;
        }
    }
}
