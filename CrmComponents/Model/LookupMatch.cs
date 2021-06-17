using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;

namespace CrmComponents.Model
{
    public class LookupMatch
    {
        public EntityModel Model { get; set; }
        public CrmCommands Commands { get; set; }
        public List<SsisInput> SsisInputs { get; set; }
        public string SsisColumnName { get; set; }
        public List<Lookup> PossibleLookups { get; set; }
        public ComplexMappingEnum MatchingType { get; set; }
        public Lookup TargetLookupEntity { get; set; }
        public List<CrmAttribute> TargetFields { get; set; } = new List<CrmAttribute>();
        public AlternateKey AlternateKey { get; set; }
        public string MatchedMultiple { get; set; }
        public string MatchNotFound { get; set; }
        public string DefaultValue { get; set; }
    }
}
