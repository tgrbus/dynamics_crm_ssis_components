using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using CrmComponents.Helpers.Enums;
using CrmComponents.Soap.Extensions;

namespace CrmComponents.Soap
{
    class TransportLayer
    {
        private SoapCrmCommands commands;

        private string appliesTo;//urn:crmemea:dynamics.com
        private string userRealmOnline = "https://login.microsoftonline.com/GetUserRealm.srf";
        private string rst2Online = "https://login.microsoftonline.com/RST2.srf";
        private string stsFederationUrl = null;
        public TransportLayer(SoapCrmCommands commands)
        {
            this.commands = commands;
        }

        private string header;

        public string Header(EndpointTypeEnum endpoint, MethodNameEnum method = MethodNameEnum.Execute)
        {
            if (DateTime.Now.ToUniversalTime() > commands.expires.AddSeconds(-30)) {
                switch (commands.connection.ConnectionType) {
                    case ConnectionTypeEnum.Online:
                        GetHeaderOnline2();
                        break;
                    case ConnectionTypeEnum.Adfs:
                        string endpointUrl = endpoint == EndpointTypeEnum.DiscoveryEndpoint ?
                            commands.connection.DiscoveryService.ToString() : commands.connection.Organization?.OrganizationService.ToString();
                        GetHeaderIfd(endpointUrl);
                        break;
                }
            }

            header = endpoint == EndpointTypeEnum.DiscoveryEndpoint ?
                header.Replace("/xrm/2011/Contracts/Services/IOrganizationService/", "/xrm/2011/Contracts/Discovery/IDiscoveryService/") :
                header.Replace("/xrm/2011/Contracts/Discovery/IDiscoveryService/", "/xrm/2011/Contracts/Services/IOrganizationService/");

            header = endpoint == EndpointTypeEnum.DiscoveryEndpoint ?
                header.Replace($"<a:To s:mustUnderstand=\"1\">{commands.connection.Organization?.OrganizationService}</a:To>", $"<a:To s:mustUnderstand=\"1\">{commands.connection.DiscoveryService}</a:To>") :
                header.Replace($"<a:To s:mustUnderstand=\"1\">{commands.connection.DiscoveryService}</a:To>", $"<a:To s:mustUnderstand=\"1\">{commands.connection.Organization?.OrganizationService}</a:To>");

            int start = header.IndexOf("<a:Action");
            int end = header.IndexOf("</a:Action>") + 11;
            header = header.Remove(start, end - start);
            if (endpoint == EndpointTypeEnum.OrganizationEndpoint) {
                header = header.Insert(start,
                    $"<a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/{method.ToString()}</a:Action>");
            }
            else {
                header = header.Insert(start,
                @"<a:Action s:mustUnderstand=""1"">http://schemas.microsoft.com/xrm/2011/Contracts/Discovery/IDiscoveryService/Execute</a:Action>");
            }


            if (endpoint == EndpointTypeEnum.OrganizationEndpoint && !header.Contains("<SdkClientVersion")) {
                int index = header.IndexOf("</a:Action>") + 11;
                header = header.Insert(index, $@"<SdkClientVersion
			xmlns=""http://schemas.microsoft.com/xrm/2011/Contracts"">{commands.connection.Organization.OrganizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}
		    </SdkClientVersion>");
            }

            return header;
        }

        public Discovery_92_Online.OrganizationDetailCollection ExecuteDiscoveryOnline(int timeoutSec)
        {
            var requestBody = @"<s:Body>
		        <Execute xmlns=""http://schemas.microsoft.com/xrm/2011/Contracts/Discovery"">
			        <request i:type=""RetrieveOrganizationsRequest"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
				        <AccessType>Default</AccessType>
				        <Release>Current</Release>
			        </request>
		        </Execute>
	        </s:Body>";
            string xml = "";
            xml += "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">";
            xml += Header(EndpointTypeEnum.DiscoveryEndpoint);
            xml += requestBody;
            xml += "</s:Envelope>";


            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(commands.connection.DiscoveryService);
            request.Timeout = timeoutSec * 1000;
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] bytesToWrite = encoding.GetBytes(xml);
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlDocument xDoc = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null) {
                    using (XmlTextReader reader = new XmlTextReader(dataStream)) {
                        xDoc.Load(reader);
                    }
                }
            }

            var rez = ((XmlElement)xDoc.GetElementsByTagName("Details")[0]);
            var s = rez.OuterXml.Replace("<Details", "<OrganizationDetailCollection").Replace("</Details>", "</OrganizationDetailCollection>");

            Discovery_92_Online.OrganizationDetailCollection coll;
            using (TextReader tr = new StringReader(s)) {
                DataContractSerializer ser = new DataContractSerializer(typeof(Discovery_92_Online.OrganizationDetailCollection));
                using (XmlReader r = XmlReader.Create(tr)) {
                    coll = (Discovery_92_Online.OrganizationDetailCollection)ser.ReadObject(r);
                }
            }

            return coll;
        }

        public IServiceClient SetWcfClient(EndpointTypeEnum endpointType = EndpointTypeEnum.OrganizationEndpoint, long maxMessageSize = 65536000, int timeoutSec = 240)
        {
            IServiceClient client;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorHandler);
            CustomBinding binding = new CustomBinding();

            if (commands.connection.DiscoveryService.Scheme == "http") {
                SymmetricSecurityBindingElement be1 = SecurityBindingElement.CreateSspiNegotiationBindingElement();
                be1.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Default;
                be1.IncludeTimestamp = true;
                be1.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
                be1.RequireSignatureConfirmation = false;
                be1.LocalClientSettings.DetectReplays = true;
                be1.LocalServiceSettings.DetectReplays = true;
                binding.Elements.Add(be1);

                binding.Elements.Add(new TextMessageEncodingBindingElement());

                var be2 = new HttpTransportBindingElement();
                be2.MaxReceivedMessageSize = maxMessageSize;
                binding.Elements.Add(be2);
            }
            else {
                TransportSecurityBindingElement be3 = SecurityBindingElement.CreateSspiNegotiationOverTransportBindingElement();
                be3.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Default;
                be3.IncludeTimestamp = true;
                be3.MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
                be3.LocalClientSettings.DetectReplays = false;
                be3.LocalServiceSettings.DetectReplays = false;
                binding.Elements.Add(be3);

                binding.Elements.Add(new TextMessageEncodingBindingElement());

                var be4 = new HttpsTransportBindingElement();
                be4.MaxReceivedMessageSize = maxMessageSize;
                binding.Elements.Add(be4);
            }

            if (endpointType == EndpointTypeEnum.OrganizationEndpoint) {
                var adClient = new Organization_91_Online.OrganizationServiceClient(binding, new EndpointAddress(commands.connection.Organization.OrganizationService));
                adClient.ClientCredentials.Windows.ClientCredential.UserName = commands.connection.Username;
                adClient.ClientCredentials.Windows.ClientCredential.Password = commands.connection.Password;
                adClient.ClientCredentials.Windows.ClientCredential.Domain = commands.connection.Domain;
                adClient.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(timeoutSec);
                client = adClient;
            }
            else {
                var adClient = new Discovery_92_Online.DiscoveryServiceClient(binding, new EndpointAddress(commands.connection.DiscoveryService));
                adClient.ClientCredentials.Windows.ClientCredential.UserName = commands.connection.Username;
                adClient.ClientCredentials.Windows.ClientCredential.Password = commands.connection.Password;
                adClient.ClientCredentials.Windows.ClientCredential.Domain = commands.connection.Domain;
                adClient.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(timeoutSec);
                client = adClient;
            }

            return client;
        }

        private static bool IgnoreCertificateErrorHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return true;
        }

        public string CreateSoapHeaderOnline(string keyIdentifier, string token1, string token2)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/xrm/2011/Contracts/Discovery/IDiscoveryService/Execute</a:Action>");
            xml.Append("<Security xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<EncryptedData Id=\"Assertion0\" Type=\"http://www.w3.org/2001/04/xmlenc#Element\" xmlns=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#tripledes-cbc\"/>");
            xml.Append("<ds:KeyInfo xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<EncryptedKey>");
            xml.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p\"/>");
            xml.Append("<ds:KeyInfo Id=\"keyinfo\">");
            xml.Append("<wsse:SecurityTokenReference xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<wsse:KeyIdentifier EncodingType=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary\" ValueType=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509SubjectKeyIdentifier\">" + keyIdentifier + "</wsse:KeyIdentifier>");
            xml.Append("</wsse:SecurityTokenReference>");
            xml.Append("</ds:KeyInfo>");
            xml.Append("<CipherData>");
            xml.Append("<CipherValue>" + token1 + "</CipherValue>");
            xml.Append("</CipherData>");
            xml.Append("</EncryptedKey>");
            xml.Append("</ds:KeyInfo>");
            xml.Append("<CipherData>");
            xml.Append("<CipherValue>" + token2 + "</CipherValue>");
            xml.Append("</CipherData>");
            xml.Append("</EncryptedData>");
            xml.Append("</Security>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + commands.connection.DiscoveryService + "</a:To>");
            xml.Append("</s:Header>");
            return xml.ToString();
        }

        public void GetHeaderIfd(string endpointUrl) {

            string adfsUrl = GetAdfs();
            if (adfsUrl == null) {
                return;
            }

            DateTime now = DateTime.Now;
            string usernamemixed = adfsUrl + "/13/usernamemixed";

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\">");
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</a:Action>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<Security s:mustUnderstand=\"1\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp  u:Id=\"" + Guid.NewGuid() + "\">");
            xml.Append("<u:Created>" + now.ToUniversalTime().ToString("o") + "</u:Created>");
            xml.Append("<u:Expires>" + now.AddMinutes(60).ToUniversalTime().ToString("o") + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<UsernameToken u:Id=\"" + Guid.NewGuid() + "\">");
            xml.Append("<Username>" + commands.connection.Username + "</Username>");
            xml.Append("<Password Type=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText\">" + commands.connection.Password + "</Password>");
            xml.Append("</UsernameToken>");
            xml.Append("</Security>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + usernamemixed + "</a:To>");
            xml.Append("</s:Header>");
            xml.Append("<s:Body>");
            xml.Append("<trust:RequestSecurityToken xmlns:trust=\"http://docs.oasis-open.org/ws-sx/ws-trust/200512\">");
            xml.Append("<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">");
            xml.Append("<a:EndpointReference>");
            xml.Append("<a:Address>" + endpointUrl + "</a:Address>");
            xml.Append("</a:EndpointReference>");
            xml.Append("</wsp:AppliesTo>");
            xml.Append("<trust:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</trust:RequestType>");
            xml.Append("</trust:RequestSecurityToken>");
            xml.Append("</s:Body>");
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(usernamemixed);
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] bytesToWrite = encoding.GetBytes(xml.ToString());
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlDocument x = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream == null)
                        return;

                    using (StreamReader reader = new StreamReader(dataStream)) {
                        x.Load(reader);
                    }
                }
            }

            XmlNodeList cipherValue1 = x.GetElementsByTagName("e:CipherValue");
            string token1 = cipherValue1[0].InnerText;

            XmlNodeList cipherValue2 = x.GetElementsByTagName("xenc:CipherValue");
            string token2 = cipherValue2[0].InnerText;

            XmlNodeList keyIdentiferElements = x.GetElementsByTagName("o:KeyIdentifier");
            string keyIdentifer = keyIdentiferElements[0].InnerText;

            XmlNodeList x509IssuerNameElements = x.GetElementsByTagName("X509IssuerName");
            string x509IssuerName = x509IssuerNameElements[0].InnerText;

            XmlNodeList x509SerialNumberElements = x.GetElementsByTagName("X509SerialNumber");
            string x509SerialNumber = x509SerialNumberElements[0].InnerText;

            XmlNodeList binarySecretElements = x.GetElementsByTagName("trust:BinarySecret");
            string binarySecret = binarySecretElements[0].InnerText;

            string created = now.AddMinutes(-1).ToUniversalTime().ToString("o");
            string expires = now.AddMinutes(60).ToUniversalTime().ToString("o");
            string timestamp = "<u:Timestamp xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" u:Id=\"_0\"><u:Created>" + created + "</u:Created><u:Expires>" + expires + "</u:Expires></u:Timestamp>";

            SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
            byte[] hashedDataBytes = sha1Hasher.ComputeHash(Encoding.UTF8.GetBytes(timestamp));
            string digestValue = Convert.ToBase64String(hashedDataBytes);

            string signedInfo = "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></CanonicalizationMethod><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#hmac-sha1\"></SignatureMethod><Reference URI=\"#_0\"><Transforms><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"></Transform></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"></DigestMethod><DigestValue>" + digestValue + "</DigestValue></Reference></SignedInfo>";
            byte[] signedInfoBytes = Encoding.UTF8.GetBytes(signedInfo);
            HMACSHA1 hmac = new HMACSHA1();
            byte[] binarySecretBytes = Convert.FromBase64String(binarySecret);
            hmac.Key = binarySecretBytes;
            byte[] hmacHash = hmac.ComputeHash(signedInfoBytes);
            string signatureValue = Convert.ToBase64String(hmacHash);

            XmlNodeList tokenExpiresElements = x.GetElementsByTagName("wsu:Expires");

            this.commands.expires = DateTime.ParseExact(tokenExpiresElements[0].InnerText, "yyyy-MM-ddTHH:mm:ss.fffK", null).ToUniversalTime();

            header = CreateSoapHeaderOnPremise(keyIdentifer, token1, token2, x509IssuerName, x509SerialNumber, signatureValue, digestValue, created, expires);

        }

        public void GetHeaderOnline2()
        {
            GetUrnOnline(); //urn:crmemea:dynamics.com
            GetUserRealmOnline();//this.stsFederationUrl
            if (this.stsFederationUrl != null) {
                XmlNode assertionNode = GetStsOnlineFederationTokens();
                GetHeaderOnlineFederation(assertionNode);
            }
            else {
                GetHeaderOnline();
            }
        }

        private void GetHeaderOnline() {
            DateTime now = DateTime.Now;

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">");
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand='1'>http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</a:Action>");

            xml.Append("<SdkClientVersion xmlns='http://schemas.microsoft.com/xrm/2011/Contracts'>");
            xml.Append("9.0");
            xml.Append("</SdkClientVersion>");

            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">https://login.microsoftonline.com/RST2.srf</a:To>");
            xml.Append("<o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp u:Id=\"_0\">");
            xml.Append("<u:Created>" + now.ToUniversalTime().ToString("o") + "</u:Created>");
            xml.Append("<u:Expires>" + now.AddMinutes(60).ToUniversalTime().ToString("o") + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<o:UsernameToken u:Id=\"uuid-" + Guid.NewGuid() + "-1\">");
            xml.Append("<o:Username>" + commands.connection.Username + "</o:Username>");
            xml.Append("<o:Password>" + commands.connection.Password + "</o:Password>");
            xml.Append("</o:UsernameToken>");
            xml.Append("</o:Security>");
            xml.Append("</s:Header>");
            xml.Append("<s:Body>");
            xml.Append("<trust:RequestSecurityToken xmlns:trust=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">");
            xml.Append("<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">");
            xml.Append("<a:EndpointReference>");
            xml.Append("<a:Address>" + this.appliesTo + "</a:Address>");
            xml.Append("</a:EndpointReference>");
            xml.Append("</wsp:AppliesTo>");
            xml.Append("<trust:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</trust:RequestType>");
            xml.Append("</trust:RequestSecurityToken>");
            xml.Append("</s:Body>");
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.rst2Online);
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] bytesToWrite = encoding.GetBytes(xml.ToString());
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlDocument x = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream == null) {
                        throw new Exception();
                    }
                    using (StreamReader reader = new StreamReader(dataStream)) {
                        x.Load(reader);
                    }
                }
            }

            XmlNodeList cipherElements = x.GetElementsByTagName("CipherValue");
            string token1 = cipherElements[0].InnerText;
            string token2 = cipherElements[1].InnerText;

            XmlNodeList keyIdentifierElements = x.GetElementsByTagName("wsse:KeyIdentifier");
            string keyIdentifier = keyIdentifierElements[0].InnerText;

            XmlNodeList tokenExpiresElements = x.GetElementsByTagName("wsu:Expires");
            string tokenExpires = tokenExpiresElements[0].InnerText;

            this.header = CreateSoapHeaderOnline(keyIdentifier, token1, token2);
            commands.expires = DateTimeOffset.Parse(tokenExpires).UtcDateTime;
        }

        public string CreateSoapHeaderOnPremise(string keyIdentifer, string token1, string token2, string issuerNameX509, string serialNumberX509, string signatureValue, string digestValue, string created, string expires2)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/Execute</a:Action>");
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">" + commands.connection.DiscoveryService + "</a:To>");
            xml.Append("<o:Security xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" u:Id=\"_0\">");
            xml.Append("<u:Created>" + created + "</u:Created>");
            xml.Append("<u:Expires>" + expires2 + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append("<xenc:EncryptedData Type=\"http://www.w3.org/2001/04/xmlenc#Element\" xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<xenc:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#aes256-cbc\"/>");
            xml.Append("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<e:EncryptedKey xmlns:e=\"http://www.w3.org/2001/04/xmlenc#\">");
            xml.Append("<e:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p\">");
            xml.Append("<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"/>");
            xml.Append("</e:EncryptionMethod>");
            xml.Append("<KeyInfo>");
            xml.Append("<o:SecurityTokenReference xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<X509Data>");
            xml.Append("<X509IssuerSerial>");
            xml.Append("<X509IssuerName>" + issuerNameX509 + "</X509IssuerName>");
            xml.Append("<X509SerialNumber>" + serialNumberX509 + "</X509SerialNumber>");
            xml.Append("</X509IssuerSerial>");
            xml.Append("</X509Data>");
            xml.Append("</o:SecurityTokenReference>");
            xml.Append("</KeyInfo>");
            xml.Append("<e:CipherData>");
            xml.Append("<e:CipherValue>" + token1 + "</e:CipherValue>");
            xml.Append("</e:CipherData>");
            xml.Append("</e:EncryptedKey>");
            xml.Append("</KeyInfo>");
            xml.Append("<xenc:CipherData>");
            xml.Append("<xenc:CipherValue>" + token2 + "</xenc:CipherValue>");
            xml.Append("</xenc:CipherData>");
            xml.Append("</xenc:EncryptedData>");
            xml.Append("<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
            xml.Append("<SignedInfo>");
            xml.Append("<CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>");
            xml.Append("<SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#hmac-sha1\"/>");
            xml.Append("<Reference URI=\"#_0\">");
            xml.Append("<Transforms>");
            xml.Append("<Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\"/>");
            xml.Append("</Transforms>");
            xml.Append("<DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\"/>");
            xml.Append("<DigestValue>" + digestValue + "</DigestValue>");
            xml.Append("</Reference>");
            xml.Append("</SignedInfo>");
            xml.Append("<SignatureValue>" + signatureValue + "</SignatureValue>");
            xml.Append("<KeyInfo>");
            xml.Append("<o:SecurityTokenReference xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<o:KeyIdentifier ValueType=\"http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID\">" + keyIdentifer + "</o:KeyIdentifier>");
            xml.Append("</o:SecurityTokenReference>");
            xml.Append("</KeyInfo>");
            xml.Append("</Signature>");
            xml.Append("</o:Security>");
            xml.Append("</s:Header>");

            return xml.ToString();
        }

        private void GetUrnOnline() {
            string uri = commands.connection.DiscoveryService.ToString();
            if (uri.LastIndexOf("/") == uri.Length - 1) {
                uri = uri.Substring(0, uri.Length - 1);
            }
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri + "?wsdl=wsdl1");
            request.Method = "GET";
            XmlDocument x = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                Stream dataStream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(dataStream)) {
                    x.Load(reader);
                }
            }

            XmlNodeList nodes = x.GetElementsByTagName("ms-xrm:AuthenticationPolicy");
            foreach (XmlNode node in nodes) {
                if (node?["ms-xrm:Authentication"]?.InnerText.ToLower() == "onlinefederation") {
                    this.appliesTo = node?["ms-xrm:SecureTokenService"]?["ms-xrm:OrgTrust"]?["ms-xrm:AppliesTo"]?.InnerText;//urn:crmemea:dynamics.com
                }
            }
        }

        private void GetUserRealmOnline() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.userRealmOnline);
            ASCIIEncoding encoding = new ASCIIEncoding();
            string body = $"login={Uri.EscapeUriString(commands.connection.Username)}&xml=1";

            byte[] bytesToWrite = encoding.GetBytes(body);
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlDocument x = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream == null) {
                        throw new Exception();
                    }
                    using (StreamReader reader = new StreamReader(dataStream)) {
                        x.Load(reader);
                    }
                }
            }

            var isFederation = Boolean.Parse(x.GetElementsByTagName("IsFederatedNS")[0].InnerText);
            if (isFederation) {
                this.stsFederationUrl = x.GetElementsByTagName("STSAuthURL")[0].InnerText;
                this.stsFederationUrl = this.stsFederationUrl.Replace("http://", "https://").Replace("2005/usernamemixed", "13/usernamemixed");
            }
        }

        private XmlNode GetStsOnlineFederationTokens() {
            DateTime now = DateTime.Now;
            string body = $@"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"" xmlns:u=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
	            <s:Header>
		            <a:Action s:mustUnderstand=""1"">http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</a:Action>
		            <a:MessageID>urn:uuid:{Guid.NewGuid()}</a:MessageID>
		            <a:ReplyTo>
			            <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
		            </a:ReplyTo>
		            <a:To s:mustUnderstand=""1"">{this.stsFederationUrl}</a:To>
		            <o:Security s:mustUnderstand=""1"" xmlns:o=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
			            <u:Timestamp u:Id=""_0"">
				            <u:Created>{now.ToUniversalTime().ToString("o")}</u:Created>
				            <u:Expires>{now.AddMinutes(60).ToUniversalTime().ToString("o")}</u:Expires>
			            </u:Timestamp>
			            <o:UsernameToken u:Id=""uuid-{Guid.NewGuid()}-1"">
				            <o:Username>{commands.connection.Username}</o:Username>
				            <o:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"">{commands.connection.Password}</o:Password>
			            </o:UsernameToken>
		            </o:Security>
	            </s:Header>
	            <s:Body>
		            <trust:RequestSecurityToken xmlns:trust=""http://docs.oasis-open.org/ws-sx/ws-trust/200512"">
			            <wsp:AppliesTo xmlns:wsp=""http://schemas.xmlsoap.org/ws/2004/09/policy"">
				            <wsa:EndpointReference xmlns:wsa=""http://www.w3.org/2005/08/addressing"">
					            <wsa:Address>urn:federation:MicrosoftOnline</wsa:Address>
				            </wsa:EndpointReference>
			            </wsp:AppliesTo>
			            <trust:KeyType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer</trust:KeyType>
			            <trust:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</trust:RequestType>
		            </trust:RequestSecurityToken>
	            </s:Body>
            </s:Envelope>";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.stsFederationUrl);
            UTF8Encoding encoding = new UTF8Encoding();

            byte[] bytesToWrite = encoding.GetBytes(body);
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=utf-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlNode assertionNode = null;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream == null)
                        throw new Exception();

                    using (StreamReader reader = new StreamReader(dataStream)) {
                        XmlDocument x = new XmlDocument();
                        x.Load(reader);
                        assertionNode = x.GetElementsByTagName("saml:Assertion")[0];
                    }
                }
            }
            return assertionNode;
        }

        private void GetHeaderOnlineFederation(XmlNode asserionNode) {
            DateTime now = DateTime.Now;

            StringBuilder xml = new StringBuilder();
            xml.Append("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:u=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\">");
            xml.Append("<s:Header>");
            xml.Append("<a:Action s:mustUnderstand='1'>http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</a:Action>");
            
            xml.Append("<a:MessageID>urn:uuid:" + Guid.NewGuid() + "</a:MessageID>");
            xml.Append("<a:ReplyTo>");
            xml.Append("<a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>");
            xml.Append("</a:ReplyTo>");
            xml.Append("<a:To s:mustUnderstand=\"1\">https://login.microsoftonline.com/RST2.srf</a:To>");
            xml.Append("<o:Security s:mustUnderstand=\"1\" xmlns:o=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">");
            xml.Append("<u:Timestamp u:Id=\"_0\">");
            xml.Append("<u:Created>" + now.ToUniversalTime().ToString("o") + "</u:Created>");
            xml.Append("<u:Expires>" + now.AddMinutes(60).ToUniversalTime().ToString("o") + "</u:Expires>");
            xml.Append("</u:Timestamp>");
            xml.Append(asserionNode.OuterXml);
            xml.Append("</o:Security>");
            xml.Append("</s:Header>");
            xml.Append("<s:Body>");
            xml.Append("<trust:RequestSecurityToken xmlns:trust=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">");
            xml.Append("<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">");
            xml.Append("<a:EndpointReference>");
            xml.Append("<a:Address>" + this.appliesTo + "</a:Address>");
            xml.Append("</a:EndpointReference>");
            xml.Append("</wsp:AppliesTo>");
            xml.Append("<trust:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</trust:RequestType>");
            xml.Append("</trust:RequestSecurityToken>");
            xml.Append("</s:Body>");
            xml.Append("</s:Envelope>");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.rst2Online);
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] bytesToWrite = encoding.GetBytes(xml.ToString());
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/soap+xml; charset=UTF-8";

            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            XmlDocument x = new XmlDocument();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (Stream dataStream = response.GetResponseStream()) {
                if (dataStream == null)
                    throw new Exception();

                using (StreamReader reader = new StreamReader(dataStream)) {
                    x.Load(reader);
                }
            }

            XmlNodeList cipherElements = x.GetElementsByTagName("CipherValue");
            string token1 = cipherElements[0].InnerText;
            string token2 = cipherElements[1].InnerText;

            XmlNodeList keyIdentifierElements = x.GetElementsByTagName("wsse:KeyIdentifier");
            string keyIdentifier = keyIdentifierElements[0].InnerText;

            XmlNodeList tokenExpiresElements = x.GetElementsByTagName("wsu:Expires");
            string tokenExpires = tokenExpiresElements[0].InnerText;

            this.header = CreateSoapHeaderOnline(keyIdentifier, token1, token2);
            commands.expires = DateTimeOffset.Parse(tokenExpires).UtcDateTime;
        }

        public string GetAdfs() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(commands.connection.DiscoveryService + "?wsdl=wsdl1");
            request.Method = "GET";

            XmlDocument x = new XmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream dataStream = response.GetResponseStream()) {
                    if (dataStream == null) {
                        return null;
                    }

                    using (StreamReader reader = new StreamReader(dataStream)) {
                        x.Load(reader);
                    }
                }
            }

            XmlNodeList nodes = x.GetElementsByTagName("ms-xrm:Identifier");
            foreach (XmlNode node in nodes) {
                return node.FirstChild.InnerText.Replace("http://", "https://");
            }

            return null;
        }
    }
}
