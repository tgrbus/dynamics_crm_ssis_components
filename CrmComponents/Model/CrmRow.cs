using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    public class CrmRow
    {
        public List<CrmColumn> Columns { get; set; } = new List<CrmColumn>();
    }
}
