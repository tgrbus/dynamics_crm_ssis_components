using System;
using System.Collections.Generic;
using System.Linq;

namespace CrmComponents.Helpers.Enums
{
    public class Dictionaries {
        private static Dictionaries instance;

        private Dictionaries() {
            SetConnectionTypes();
            SetServiceTypes();
            SetComplexMapping();
            SetErrorHandlingNames();
            SetMatchingNames();
            SetActivityParties();
        }

        public List<Tuple<int, ServiceTypeEnum, string>> ConnectionTypeNames { get; private set; }
        public Dictionary<int, string> ServiceTypeNames { get; private set; }
        public Dictionary<int, string> ComplexMappingNames { get; private set; }
        public Dictionary<int, string> ErrorHandlingNames { get; private set; }
        public Dictionary<int, string> MatchingNames { get; private set; }
        public Dictionary<int, string> ActivityPartyLogicalNames { get; private set; }

        public int GetEnumFromString(string name) {
            return Dictionaries.GetInstance().ComplexMappingNames.First(n => n.Value == name).Key;
        }

        public static Dictionaries GetInstance() {
            return instance ?? (instance = new Dictionaries());
        }

        private void SetServiceTypes() {
            ServiceTypeNames = new Dictionary<int, string>();
            ServiceTypeNames.Add((int)ServiceTypeEnum.SOAP, "SOAP WCF Endpoint");
            ServiceTypeNames.Add((int)ServiceTypeEnum.REST, "REST WebAPI");
        }

        private void SetActivityParties() {
            ActivityPartyLogicalNames = new Dictionary<int, string>();

        }

        private void SetConnectionTypes() {
            ConnectionTypeNames = new List<Tuple<int, ServiceTypeEnum, string>>();
            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.Ad, ServiceTypeEnum.SOAP, "Onpremise"));
            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.Adfs, ServiceTypeEnum.SOAP, "IFD"));
            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.Online, ServiceTypeEnum.SOAP, "Online"));

            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.ClientCredentials, ServiceTypeEnum.REST, "Client Credentials"));
            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.Certificate, ServiceTypeEnum.REST, "Certificate"));
            ConnectionTypeNames.Add(new Tuple<int, ServiceTypeEnum, string>((int)ConnectionTypeEnum.Password, ServiceTypeEnum.REST, "Password"));

            ConnectionTypeNames = ConnectionTypeNames.OrderBy(n => n.Item1).ToList();
        }

        private void SetComplexMapping() {
            ComplexMappingNames = new Dictionary<int, string>();
            ComplexMappingNames.Add((int)ComplexMappingEnum.PrimaryKey, "Primary Key");
            ComplexMappingNames.Add((int)ComplexMappingEnum.AlternateKey, "Alternate Key");
            ComplexMappingNames.Add((int)ComplexMappingEnum.Manual, "Manually Specify");
        }

        private void SetErrorHandlingNames() {
            ErrorHandlingNames = new Dictionary<int, string>();
            ErrorHandlingNames.Add((int)ErrorHandlingEnum.Fail, "Fail on Error");
            ErrorHandlingNames.Add((int)ErrorHandlingEnum.Redirect, "Redirect Row");
            ErrorHandlingNames.Add((int)ErrorHandlingEnum.Ignore, "Ignore Error");
        }

        private void SetMatchingNames() {
            MatchingNames = new Dictionary<int, string>();
            MatchingNames.Add((int)MatchingEnum.Ignore, "Ignore");
            MatchingNames.Add((int)MatchingEnum.DefaultValue, "Default Value");
            MatchingNames.Add((int)MatchingEnum.MatchOne, "Match One");
            MatchingNames.Add((int)MatchingEnum.RaiseError, "Raise Error");
            MatchingNames.Add((int)MatchingEnum.UpdateAll, "Update All");
        }
    }
}
