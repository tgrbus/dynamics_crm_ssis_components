using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Model
{
    public class OrganizationRequestModel : ICloneable
    {
        public Entity Entity { get; set; }
        public Guid? PrimaryId { get; set; }
        public List<Guid> FoundedIds { get; set; } = new List<Guid>();
        public List<CrmColumn> AlternateKeys { get; set; } = new List<CrmColumn>();
        public CrmRow Row { get; set; }
        public int? RequestIndex { get; set; }
        public int Index { get; set; }
        public OrganizationResponseModel Response { get; set; } = null;
        public int BufferRowPosition { get; set; }

        public OrganizationRequestModel() {
            PrimaryId = null;
            FoundedIds = new List<Guid>();
        }

        public object Clone() {
            return this.MemberwiseClone();
        }
    }
}
