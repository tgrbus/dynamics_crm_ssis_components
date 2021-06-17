using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrmComponents.Model
{
    public class Organization
    {
        public decimal OrganizationVersion { get; set; }
        public Guid OrganizationId { get; set; }
        public string FriendlyName { get; set; }
        public string UniqueName { get; set; }
        public string UrlName { get; set; }
        public Uri OrganizationService { get; set; }
        public string ServerUrl { get; set; }
    }
}
