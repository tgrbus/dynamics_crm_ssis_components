using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    public class FetchBatch
    {
        public List<CrmRow> Rows { get; set; } = new List<CrmRow>();
        public bool MoreRecords { get; set; }
        public string PagingCookie { get; set; }
    }
}
