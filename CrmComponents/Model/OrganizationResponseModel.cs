using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    public class OrganizationResponseModel
    {
        public bool Success { get; set; }
        public Guid? Id { get; set; }
        public bool? RecordCreated { get; set; } = null;
        public int ResponseIndex { get; set; }
        public string ErrorMessage { get; set; }
        public List<Guid> ComputedGuids { get; set; }

        public OrganizationResponseModel() {
            Id = null;
        }
    }
}
