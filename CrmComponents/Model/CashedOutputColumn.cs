using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrmComponents.Model
{
    public class CashedOutputColumn
    {
        public int ColumnId { get; set; }
        public object Value { get; set; }
        public bool IsBlob { get; set; } = false;
    }
}
