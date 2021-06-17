using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Rest
{
    public class RestHttpResponse {
        public int ResponseCode;
        public List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>>();
        public string ResponseString;
    }
}
