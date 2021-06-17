using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CrmComponents.Helpers;
using CrmComponents.Helpers.Enums;
using CrmComponents.Rest;
using CrmComponents.Soap;

namespace CrmComponents.Model
{
    public class Connection
    {
        public ServiceTypeEnum ServiceType { get; set; }
        public string Username { get; set; } //txtUN,txtUsr2
        public string Password { get; set; } //txtPSW, txtPass2
        public string Domain { get; set; } //txtDMN
        public Uri DiscoveryService { get; set; } //cmbDisc, cmbDisc2
        public ConnectionTypeEnum ConnectionType { get; set; }
        public Organization Organization { get; set; } //cmbOrg, cmbOrg2
        public int? TimeoutSec { get; set; }
        public string AuthorizationServer { get; set; }//txrAuthSrv, https://login.microsoftonline.com/
        public string ClientId { get; set; }//txtAppId
        public string ServerUrl { get; set; }//txtCrmSrv
        public string ClientSecret { get; set; }//txtSecrt
        public string CertificateThumbprint { get; set; }//txtCert

        private string replaceSemicolon = ":_:";

        public string ConnectionString() {
            if(ServiceType == ServiceTypeEnum.SOAP) {
                return SoapConnectionString();
            }

            return RestConnectionString();
        }

        string SoapConnectionString() {
            string connectionString = $"{(int)ServiceType};{(int)ConnectionType};{EncodeString(DiscoveryService.ToString())};" + //0,1,2
                                      $"{EncodeString(Organization?.FriendlyName)};{EncodeString(Organization?.OrganizationService?.ToString())};" + //3,4
                                      $"{EncodeString(Organization?.OrganizationVersion.ToString("#.#", NumberFormat.NFI.nfi) ?? "")};" +
                                      $"{EncodeString(Username)};{EncodeString(Password)};{EncodeString(Domain)};" +
                                      $"{(TimeoutSec.HasValue ? TimeoutSec.Value.ToString() : "120")}";
            return connectionString;
        }

        string RestConnectionString() {
            string connectionString = $"{(int)ServiceType};{(int)ConnectionType};" + //0,1
                                      $"{EncodeString(AuthorizationServer)};{EncodeString(ClientId)};{EncodeString(ClientSecret)};" + //2,3,4
                                      $"{EncodeString(Username)};{EncodeString(Password)};" + //5,6
                                      $"{EncodeString(CertificateThumbprint)};{(TimeoutSec.HasValue ? TimeoutSec.Value.ToString() : "120")};" + //7,8
                                      $"{EncodeString(ServerUrl)}"; //9
            return connectionString;
        }

        public Connection() {

        }

        public Connection(string connectionString) {
            if (!string.IsNullOrEmpty(connectionString)) {
                string[] encodedParts = connectionString.Split(';');
                string[] parts = new string[encodedParts.Length];
                for(int i = 0; i < encodedParts.Length; i++) {
                    parts[i] = DecodeString(encodedParts[i]);
                }
                ServiceType = (ServiceTypeEnum)Convert.ToInt32(parts[0]);
                if (!string.IsNullOrEmpty(parts[1])) {
                    ConnectionType = (ConnectionTypeEnum)Convert.ToInt32(parts[1]);
                }
                if (ServiceType == ServiceTypeEnum.SOAP) {
                    DiscoveryService = string.IsNullOrEmpty(parts[2]) ? null : new Uri(parts[2]);
                    if(!string.IsNullOrEmpty(parts[3]) && !string.IsNullOrEmpty(parts[4])) {
                        Organization = new Organization {
                            FriendlyName = parts[3],
                            OrganizationService = new Uri(parts[4]),
                            OrganizationVersion = Convert.ToDecimal(parts[5], NumberFormat.NFI.nfi)};
                    }
                    Username = parts[6];
                    Password = parts[7];
                    Domain = parts[8];
                    TimeoutSec = Int32.Parse(parts[9]);
                } else {
                    AuthorizationServer = parts[2];
                    ClientId = parts[3];
                    ClientSecret = parts[4];
                    //DiscoveryService = string.IsNullOrEmpty(parts[8]) ? null : new Uri(parts[8]);
                    Username = parts[5];
                    Password = parts[6];
                    CertificateThumbprint = parts[7];
                    TimeoutSec = Int32.Parse(parts[8]);
                    ServerUrl = parts[9];
                }
            }
        }

        string EncodeString(string text) {
            if(string.IsNullOrEmpty(text)) {
                return "";
            }

            text = text.Replace(";", replaceSemicolon);
            return XmlConvert.EncodeName(text);
        }

        string DecodeString(string text) {
            if(string.IsNullOrEmpty(text)) {
                return "";
            }

            text = XmlConvert.DecodeName(text);
            return text.Replace(replaceSemicolon, ";");
        }

        public static CrmCommands CrmCommandsFactory(Connection conn) {
            if(conn.ServiceType == ServiceTypeEnum.SOAP) {
                return new SoapCrmCommands(conn);
            } else {
                return new RestCrmCommands(conn);
            }
        }

        public static CrmCommands CrmCommandsFactory(string connectionString) {
            Connection conn = new Connection(connectionString);
            return CrmCommandsFactory(conn);
        }
    }
}
