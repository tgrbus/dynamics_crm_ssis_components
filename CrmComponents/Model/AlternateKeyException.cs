using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    public class AlternateKeyException : Exception
    {
        public AlternateKeyException(string errorMessage) : base(errorMessage) {
        }
    }
}
