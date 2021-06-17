using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using CrmComponents.Helpers;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;
using Newtonsoft.Json.Linq;

namespace CrmComponents.Rest
{
    public class RestHelpers
    {
        DateTime tokenExpires = DateTime.MinValue;
        private string token = null;
        public Connection connection;
        private string authorizationUri = null;
        private string resourceId = null;

        private string Token {
            get {
                if(tokenExpires < DateTime.Now.ToLocalTime().AddSeconds(-30)) {
                    GetToken();
                }
                return token;
            }
        }

        public RestHelpers(Connection connection) {
            this.connection = connection;
            this.connection.ServerUrl = this.connection.ServerUrl.EndsWith("/") ? this.connection.ServerUrl.Substring(0, this.connection.ServerUrl.Length - 1) : this.connection.ServerUrl;
            connection.Organization = new Organization { OrganizationVersion = 8.0m };
            GetAuthorizationUri();
            GetOrganizationVersion();
        }

        void GetToken() {
            if(string.IsNullOrEmpty(authorizationUri)) {
                //GetAuthorizationUri();
                //GetOrganizationVersion();
                //return;
            }

            string body ="";
            switch(connection.ConnectionType) {
                case ConnectionTypeEnum.ClientCredentials:
                    body = $"grant_type=client_credentials&client_id={connection.ClientId}&client_secret={connection.ClientSecret}&resource={resourceId}";
                    break;
                case ConnectionTypeEnum.Password:
                    body = $"grant_type=password&client_id={connection.ClientId}&username={connection.Username}&password={connection.Password}&resource={resourceId}";
                    break;
            }

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(authorizationUri);
            request.Method = "POST";
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] bytesToWrite = encoding.GetBytes(body);
            request.Method = "POST";
            request.ContentLength = bytesToWrite.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            using (Stream newStream = request.GetRequestStream()) {
                newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            string responseString;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using(Stream dataStream = response.GetResponseStream()) {
                    using(StreamReader reader = new StreamReader(dataStream)) {
                        responseString = reader.ReadToEnd();
                    }
                }
            }

            JObject json = JObject.Parse(responseString);
            token = (string)json["access_token"];
            int expiresIn = (int)json["expires_in"];
            tokenExpires = DateTime.Now.ToLocalTime().AddSeconds(expiresIn);
        }

        void GetAuthorizationUri() {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(connection.ServerUrl + "/api/data/v8.0/RetrieveVersion");
            request.Method = "GET";
            try {
                using(HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                }
            } catch(System.Net.WebException ex) {
                for(int i = 0; i < ex.Response.Headers.Count; i++) {
                    var key = ex.Response.Headers.Keys[i];
                    var value = ex.Response.Headers[i];
                    if(key.ToLower() == "www-authenticate") {
                        Regex regex1 = new Regex(@"(?<=authorization_uri=)(.*?)(?=[,\s]|$)");
                        authorizationUri = regex1.Match(value).Groups[0].Value;
                        authorizationUri = authorizationUri.Replace("/authorize", "");
                        authorizationUri = authorizationUri + "/token";
                        Regex regex2 = new Regex(@"(?<=resource_id=)(.*?)(?=[,\s]|$)");
                        resourceId = regex2.Match(value).Groups[0].Value;
                        resourceId = resourceId.EndsWith("/") ? resourceId.Substring(0, resourceId.Length - 1) : resourceId;
                        resourceId = WebUtility.UrlEncode(resourceId);
                    }
                }
            }
        }

        void GetOrganizationVersion() {
            RestHttpResponse response = GetAuthRequest("RetrieveVersion", "GET");
            JObject json = JObject.Parse(response.ResponseString);
            string version = (string)json["Version"];
            int firstDotPosition = version.IndexOf(".");
            int secondDotPosition = -1;
            if (firstDotPosition >= 0) {
                secondDotPosition = version.IndexOf(".", firstDotPosition + 1);
            }

            int length = secondDotPosition > firstDotPosition ? secondDotPosition : (firstDotPosition > -1 ? firstDotPosition : version.Length);
            string versionNormalized = version.Substring(0, length);

            connection.Organization.OrganizationVersion = Convert.ToDecimal(versionNormalized, NumberFormat.NFI.nfi);
        }

        public RestHttpResponse GetAuthRequest(string message, string method = "GET", string body = null, List<KeyValuePair<string, string>> headers = null) {
            string token = Token;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(connection.ServerUrl + $"/api/data/v{connection.Organization.OrganizationVersion.ToString("0.0", NumberFormat.NFI.nfi)}/{message}");
            request.Method = method.ToUpper();
            request.Headers.Add("Authorization", "Bearer " + token);
            if(headers != null) {
                foreach(KeyValuePair<string, string> pair in headers) {
                    if(pair.Key.ToLower() == "accept") {
                        request.Accept = pair.Value;
                    } else if(pair.Key.ToLower() == "content-type") {
                        request.ContentType = pair.Value;
                    }
                    else {
                        request.Headers.Add(pair.Key, pair.Value);
                    }
                }
            }

            if(!string.IsNullOrEmpty(body)) {
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] bytesToWrite = encoding.GetBytes(body);
                request.ContentLength = bytesToWrite.Length;
                using (Stream newStream = request.GetRequestStream()) {
                    newStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                }
            }

            RestHttpResponse resp = new RestHttpResponse();
            
            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    resp.ResponseCode = (int)response.StatusCode;
                    for(int i = 0; i < response.Headers.Count; i++) {
                        string key = response.Headers.Keys[i];
                        string value = response.Headers[i];
                        resp.Headers.Add(new KeyValuePair<string, string>(key, value));
                    }

                    using(Stream dataStream = response.GetResponseStream()) {
                        using(StreamReader reader = new StreamReader(dataStream, Encoding.UTF8)) {
                            resp.ResponseString = reader.ReadToEnd();
                        }
                    }
                }
            } catch(WebException ex) {
                var response = (HttpWebResponse)ex.Response;
                resp.ResponseCode = (int)response.StatusCode;
                using (Stream dataStream = response.GetResponseStream()) {
                    using (StreamReader reader = new StreamReader(dataStream, Encoding.UTF8)) {
                        resp.ResponseString = reader.ReadToEnd();
                    }
                }
            }

            return resp;
        }
    }
}
