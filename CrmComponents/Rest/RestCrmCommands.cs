using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CrmComponents.Helpers;
using CrmComponents.Helpers.Enums;
using CrmComponents.Model;
using CrmComponents.Organization_91_Online;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Newtonsoft.Json.Linq;
using Entity = CrmComponents.Model.Entity;
using Relationship = CrmComponents.Model.Relationship;

namespace CrmComponents.Rest
{
    public class RestCrmCommands : CrmCommands {
        private RestHelpers helper;

        public RestCrmCommands(Connection connection) {
            helper = new RestHelpers(connection);
        }

        public RestCrmCommands(string connectionString) : this(new Connection(connectionString)) {}

        public override List<Organization> RetrieveOrganizations(int timeoutSec = 30) {
            throw new NotImplementedException();
        }

        public override bool WhoAmI(int timeoutSec = 30) {
            string message = "WhoAmI()";
            var response = helper.GetAuthRequest(message, "GET");
            JObject jsonResponse = JObject.Parse(response.ResponseString);
            try {
                var userId = jsonResponse["UserId"];
                Guid userGuid = (Guid)userId;
                return true;
            } catch {
                return false;
            }
        }

        public override List<Entity> RetrieveEntityList() {
            string message = "EntityDefinitions";
            string query = "?$select=IsIntersect,LogicalName,ObjectTypeCode,PrimaryNameAttribute,PrimaryIdAttribute,LogicalCollectionName,EntitySetName,IsValidForAdvancedFind,CollectionSchemaName";
            var response = helper.GetAuthRequest(message + query, "GET");
            if(response.ResponseCode != 200) {
                throw new Exception($"Error retrieving entity list. Response code = {response.ResponseCode}, message={response.ResponseString}");
            }
            JObject jsonResponse = JObject.Parse(response.ResponseString);
            JToken value = jsonResponse["value"];
            List<JToken> entities = value.Children().ToList();

            List<Entity> entityList = new List<Entity>();
            foreach (JToken e in entities) {
                Entity entity = new Entity();
                entity.LogicalName = (string)e["LogicalName"];
                entity.EntitySetName = (string)e["EntitySetName"];
                entity.IdName = (string)e["PrimaryIdAttribute"];
                entity.IsIntersect = (bool)e["IsIntersect"];
                entity.ValidForAdvancedFind = (bool)e["IsValidForAdvancedFind"];
                entityList.Add(entity);
            }

            return entityList;
        }

        public override List<CrmAttribute> RetrieveAttributesList(EntityModel model, Entity entity) {
            string message = "RetrieveEntity(LogicalName=@p0,EntityFilters=@p1,MetadataId=@p2,RetrieveAsIfPublished=@p3)";
            message += $"?@p0={WebUtility.UrlEncode("'" + entity.LogicalName + "'")}" +
                       $"&@p1={WebUtility.UrlEncode("Microsoft.Dynamics.CRM.EntityFilters'All'")}";
            message += $"&@p2=00000000-0000-0000-0000-000000000000&@p3=false";

            RestHttpResponse response = helper.GetAuthRequest(message);
            if(response.ResponseCode != 200) {
                throw new Exception($"Error retrieving attribute list for entity {entity.LogicalName}. Http code {response.ResponseCode}. Message = {response.ResponseString}");
            }
            JObject jsonResponse = JObject.Parse(response.ResponseString);
            JToken entityMetadata = jsonResponse["EntityMetadata"];
            if(entity != null) {
                entity.IdName = (string)entityMetadata["PrimaryIdAttribute"];
            }

            List<JToken> attributes = entityMetadata["Attributes"].Children().ToList();
            List<JToken> manyToOne = entityMetadata["ManyToOneRelationships"].Children().ToList();
            List<CrmAttribute> crmAttributes = new List<CrmAttribute>();

            foreach(JToken attribute in attributes) {
                if((bool)attribute["IsValidForRead"] || (bool)attribute["IsValidForCreate"] || (bool)attribute["IsValidForUpdate"]) {
                    CrmAttribute a = new CrmAttribute();
                    a.LogicalName = (string)attribute["LogicalName"];
                    var yy = attribute?["DisplayName"]?["UserLocalizedLabel"];
                    a.DisplayName = yy != null && yy.HasValues ? (string)yy["Label"] : null;
                    a.AttributeOf = (string)attribute["AttributeOf"];
                    string yomi = (string)attribute["YomiOf"];
                    if(!string.IsNullOrEmpty(a.AttributeOf) && !string.IsNullOrEmpty(yomi)) {
                        continue;
                    }
                    AttributeTypeEnum type;
                    if(Enum.TryParse((string)attribute["AttributeType"], out type)) {
                        a.CrmAttributeType = type;
                    }

                    if(a.CrmAttributeType == AttributeTypeEnum.Virtual) {
                        var typeName = (string)attribute["AttributeTypeName"]["Value"];
                        if(typeName == "MultiSelectPicklistType") {
                            a.CrmAttributeType = AttributeTypeEnum.MultiSelectPicklist;
                        }
                    }

                    a.DisplayForRead = (bool?)attribute["IsValidForRead"] ?? false;
                    if((bool)attribute["IsPrimaryId"] || a.CrmAttributeType == AttributeTypeEnum.State || a.CrmAttributeType == AttributeTypeEnum.Status) {
                        a.DisplayForCreate = a.DisplayForUpdate = true;
                    } else {
                        a.DisplayForCreate = string.IsNullOrEmpty(a.AttributeOf) && ((bool?)attribute["IsValidForCreate"] ?? false);
                        a.DisplayForUpdate = string.IsNullOrEmpty(a.AttributeOf) && ((bool?)attribute["IsValidForUpdate"] ?? false);
                    }

                    a.Length = 0;
                    a.Precision = 0;
                    a.Scale = 0;
                    a.Entity = entity.LogicalName;

                    switch(a.CrmAttributeType) {
                        case AttributeTypeEnum.Picklist:
                        case AttributeTypeEnum.State:
                        case AttributeTypeEnum.Status:
                            var x1 = attribute["OptionSet"];
                            var x2 = x1["Options"].Children().ToList();
                            a.OptionSetValues = new Dictionary<int, List<string>>();
                            foreach(JToken jToken in x2) {
                                int value = (int)jToken["Value"];
                                List<string> labels = jToken["Label"]["LocalizedLabels"].Children().Select(n => ((string)n["Label"]).ToLower()).ToList();
                                a.OptionSetValues.Add(value, labels);
                            }
                            break;
                        case AttributeTypeEnum.DateTime:
                            a.Scale = 7;
                            break;
                        case AttributeTypeEnum.Decimal:
                        case AttributeTypeEnum.Money:
                            a.Scale = (int?)attribute["Precision"] ?? 0;
                            a.Precision = 38;
                            break;
                        case AttributeTypeEnum.Lookup:
                        case AttributeTypeEnum.Customer:
                        case AttributeTypeEnum.Owner:
                            a.PossibleLookups = new List<Lookup>();
                            var custom = (bool)attribute["IsCustomAttribute"];
                            if(custom) {
                                SetLookups(model, a, manyToOne);
                            } else {
                                var targetsArray = attribute["Targets"] as JArray;
                                foreach(JToken token in targetsArray) {
                                    string targetEntityName = (string)token;
                                    Lookup l = new Lookup {
                                        LookupOdataName = a.LogicalName,
                                        TargetEntityName = targetEntityName,
                                        TargetEntitySetName = model.EntityList.First(n => n.LogicalName == targetEntityName).EntitySetName
                                    };
                                    a.PossibleLookups.Add(l);
                                }
                            }

                            break;
                        case AttributeTypeEnum.String:
                            a.Length = (int?)attribute["MaxLength"] ?? 1;
                            break;
                        case AttributeTypeEnum.EntityName:
                            a.Length = 64;
                            break;
                        case AttributeTypeEnum.Memo:
                            a.Length = (int?)attribute["MaxLength"] ?? 1;
                            break;
                        case AttributeTypeEnum.MultiSelectPicklist:
                            a.Length = 255;
                            break;
                        case AttributeTypeEnum.Virtual:
                            a.Length = 255;
                            if(((string)attributes.FirstOrDefault(n => (string)n["LogicalName"] == a.AttributeOf)["AttributeTypeName"]["Value"]).ToLower() == "multiselectpicklisttype") {
                                a.Length = 4000;
                            }
                            break;
                        case AttributeTypeEnum.PartyList:
                            break;
                    }

                    crmAttributes.Add(a);
                }
            }

            if(entity != null) {
                var keysEnumerable = entityMetadata["Keys"].Children();
                if(keysEnumerable.Any()) {
                    entity.AlternateKeys = new List<AlternateKey>();
                    foreach(JToken token in keysEnumerable) {
                        AlternateKey key = new AlternateKey { EntityLogicalName = entity.LogicalName, EntitySetName = entity.EntitySetName, KeyColumns = new List<CrmAttribute>() };
                        key.LogicalName = (string)token["LogicalName"];
                        var keyAttributes = token["KeyAttributes"].Children();
                        foreach(JToken keyAttribute in keyAttributes) {
                            key.KeyColumns.Add(new CrmAttribute {
                                LogicalName = (string)keyAttribute,
                                CrmAttributeType = crmAttributes.First(n => n.LogicalName == (string)keyAttribute).CrmAttributeType
                            });
                        }
                        entity.AlternateKeys.Add(key);
                    }
                }
            }

            if((bool)entityMetadata["IsIntersect"]) {
                var manyToManyAll = entityMetadata["ManyToManyRelationships"].Children();
                var manyToMany = manyToManyAll.First(n => (string)n["IntersectEntityName"] == entity.LogicalName);
                CrmAttribute a1 = new CrmAttribute();
                a1.LogicalName = (string)manyToMany["Entity1IntersectAttribute"];
                a1.CrmAttributeType = AttributeTypeEnum.Lookup;
                a1.Entity = entity.LogicalName;
                a1.DisplayForRead = a1.DisplayForCreate = true;
                a1.DisplayForUpdate = false;
                Lookup l1 = new Lookup();
                l1.LookupOdataName = l1.TargetEntityName = (string)manyToMany["Entity1LogicalName"];
                l1.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l1.TargetEntityName).EntitySetName;
                a1.PossibleLookups.Add(l1);

                CrmAttribute a2 = new CrmAttribute();
                a2.LogicalName = (string)manyToMany["Entity2IntersectAttribute"];
                a2.CrmAttributeType = AttributeTypeEnum.Lookup;
                a2.Entity = entity.LogicalName;
                a2.DisplayForRead = a2.DisplayForCreate = true;
                a2.DisplayForUpdate = false;
                Lookup l2 = new Lookup();
                l2.LookupOdataName = l2.TargetEntityName = (string)manyToMany["Entity2LogicalName"];
                l2.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l2.TargetEntityName).EntitySetName;
                a2.PossibleLookups.Add(l2);

                var forRemove = crmAttributes.Where(n => n.LogicalName == a1.LogicalName || n.LogicalName == a2.LogicalName).ToList();
                crmAttributes.Remove(forRemove[0]);
                crmAttributes.Remove(forRemove[1]);
                crmAttributes.AddRange(new [] {a1, a2});

                var primaryKey = crmAttributes.First(n => n.CrmAttributeType == AttributeTypeEnum.Uniqueidentifier);
                primaryKey.DisplayForCreate = primaryKey.DisplayForUpdate = false;

                if(entity != null) {
                    entity.IntersectRelationship = new Relationship {
                        Entity1AttributeName = (string)manyToMany["Entity1IntersectAttribute"],
                        Entity1EntityName = (string)manyToMany["Entity1LogicalName"],
                        Entity1EntitySetName = l1.TargetEntitySetName,
                        Entity2AttributeName = (string)manyToMany["Entity2IntersectAttribute"],
                        Entity2EntityName = (string)manyToMany["Entity2LogicalName"],
                        Entity2EntitySetName = l2.TargetEntitySetName,
                        Name = (string)manyToMany["Entity1NavigationPropertyName"]
                    };
                }
            }

            crmAttributes = crmAttributes.OrderBy(n => n.LogicalName).ToList();

            return crmAttributes;
        }

        private void SetLookups(EntityModel model, CrmAttribute attribute, List<JToken> manyToOneList) {
            var targetRelationships = manyToOneList.Where(n => (string)n["ReferencingAttribute"] == attribute.LogicalName);
            foreach(JToken token in targetRelationships) {
                Lookup l1 = new Lookup();
                l1.TargetEntityName = (string)token["ReferencedEntity"];
                l1.LookupOdataName = (string)token["ReferencingEntityNavigationPropertyName"];
                l1.TargetEntitySetName = model.EntityList.First(n => n.LogicalName == l1.TargetEntityName).EntitySetName;
                attribute.PossibleLookups.Add(l1);
            }
        }

        public override FetchBatch RetrieveAllRecords(string entityName, string entitySetName, List<CrmAttribute> attributes, List<CrmAttribute> additionalAttributes = null, 
                                                      int pageNumber = 1, string cookie = null, int count = 1000, bool allColumns = false) {
            List<CrmAttribute> allAttributes = attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf)).ToList();
            if(additionalAttributes != null) {
                allAttributes.AddRange(additionalAttributes);
            }

            XmlDocument fetchXml = new XmlDocument();
            var root_f = fetchXml.CreateElement("fetch");
            fetchXml.AppendChild(root_f);
            var entity_f = fetchXml.CreateElement("entity");
            entity_f.SetAttribute("name", entityName);
            root_f.AppendChild(entity_f);
            if(allColumns) {
                var attribute_f = fetchXml.CreateElement("all-attributes");
                entity_f.AppendChild(attribute_f);
            } else {
                var attributesAll = allAttributes.OrderBy(n => n.LogicalName).ToList();
                foreach (CrmAttribute attr in attributesAll) {
                    var attribute_f = fetchXml.CreateElement("attribute");
                    attribute_f.SetAttribute("name", attr.LogicalName);
                    entity_f.AppendChild(attribute_f);
                }
            }

            root_f.SetAttribute("count", count.ToString());
            root_f.SetAttribute("page", pageNumber.ToString());
            if(!string.IsNullOrEmpty(cookie)) {
                root_f.SetAttribute("paging-cookie", cookie);
            }

            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            headers.AddRange(new [] {
                new KeyValuePair<string, string>("Accept", "application/json;odata.metadata=minimal"),
                new KeyValuePair<string, string>("Accept-Charset", "UTF-8"), 
                new KeyValuePair<string, string>("Content-Type", "multipart/mixed;boundary=batch_divider"), 
                new KeyValuePair<string, string>("MSCRMCallerID", "00000000-0000-0000-0000-000000000000"), 
                new KeyValuePair<string, string>("OData-MaxVersion", "4.0"),
                new KeyValuePair<string, string>("OData-Version", "4.0"), 
                new KeyValuePair<string, string>("Cache-Control", "no-cache"), 
            });

            string body =
                    $@"--batch_divider
Content-Type: application/http
Content-Transfer-Encoding:binary

GET {helper.connection.ServerUrl}/api/data/v{helper.connection.Organization.OrganizationVersion.ToString(NumberFormat.NFI.nfi)}/{entitySetName}?fetchXml={WebUtility.UrlEncode(fetchXml.InnerXml)} HTTP/1.1
Content-Type: application/json
OData-Version: 4.0
OData-MaxVersion: 4.0
Prefer: odata.include-annotations=*


--batch_divider--";

            var response = helper.GetAuthRequest("$batch", "POST", body, headers);
            if(response.ResponseCode != 200) {
                throw new Exception($"Error retrieving {entityName} records. Http code: {response.ResponseCode}, Message:{response.ResponseString}");
            }
            int firstIndex = response.ResponseString.IndexOf("{");
            int lastIndex = response.ResponseString.LastIndexOf("}");
            JObject jsonResponse = JObject.Parse(response.ResponseString.Substring(firstIndex, lastIndex - firstIndex + 1));
            JToken v = jsonResponse["value"];
            List<JToken> records = v.Children().ToList();
            string nextCookie = (string)jsonResponse["@Microsoft.Dynamics.CRM.fetchxmlpagingcookie"];

            FetchBatch batch = new FetchBatch();
            batch.MoreRecords = !string.IsNullOrEmpty(nextCookie);
            if(!string.IsNullOrEmpty(nextCookie)) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(nextCookie);
                string encoded = doc.FirstChild.Attributes["pagingcookie"].Value;
                batch.PagingCookie = WebUtility.UrlDecode(encoded);
                batch.PagingCookie = WebUtility.UrlDecode(batch.PagingCookie);
            }

            foreach(JToken record in records) {
                CrmRow row = new CrmRow();
                foreach(CrmAttribute crmAttribute in attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf))) {
                    CrmColumn col = new CrmColumn { CrmType = crmAttribute.CrmAttributeType, LogicalName = crmAttribute.LogicalName, Value = null };
                    var valueToken = crmAttribute.CrmAttributeType == AttributeTypeEnum.Lookup 
                                     || crmAttribute.CrmAttributeType == AttributeTypeEnum.Owner || crmAttribute.CrmAttributeType == AttributeTypeEnum.Customer
                        ? $"_{crmAttribute.LogicalName}_value"
                        : crmAttribute.LogicalName;
                    col.Value = ConvertJsonToCsharpObject(record, crmAttribute);
                    row.Columns.Add(col);
                }

                foreach(CrmAttribute crmAttribute in attributes.Where(n => !string.IsNullOrEmpty(n.AttributeOf))) {
                    CrmAttribute originalAttribute = allAttributes.FirstOrDefault(n => n.LogicalName == crmAttribute.AttributeOf);
                    if(originalAttribute != null) {
                        CrmColumn col = new CrmColumn { CrmType = crmAttribute.CrmAttributeType, LogicalName = crmAttribute.LogicalName, Value = null };
                        var valueToken = originalAttribute.CrmAttributeType == AttributeTypeEnum.Lookup || originalAttribute.CrmAttributeType == AttributeTypeEnum.Owner
                                                                                                        || originalAttribute.CrmAttributeType == AttributeTypeEnum.Customer
                            ? $"_{originalAttribute.LogicalName}_value"
                            : originalAttribute.LogicalName;
                        if(record[valueToken] != null) {
                            string columnName;
                            if(crmAttribute.CrmAttributeType == AttributeTypeEnum.EntityName) {
                                columnName = valueToken + "@Microsoft.Dynamics.CRM.lookuplogicalname";
                            } else {
                                columnName = valueToken + "@OData.Community.Display.V1.FormattedValue";
                            }
                            col.Value = (string)record[columnName];
                        }
                        row.Columns.Add(col);
                    }
                }
                batch.Rows.Add(row);
            }

            return batch;
        }

        string ExtractParties(JToken record, CrmAttribute attribute) {
            string result = null;
            List<string> records = new List<string>();

            var childs = record.Children().ToList();
            var typeCode = (int)(ActivityPartyTypeEnum)Enum.Parse(typeof(ActivityPartyTypeEnum), attribute.NotAliasedName, true);

            foreach(JToken ch in childs) {
                var cc = ch.Children().ToList();
                if(cc.Count > 0) {
                    foreach(JToken child in cc.Children()) {
                        int? typeMask = (int?)child["participationtypemask"];
                        if(typeMask == typeCode) {
                            Guid partyId = (Guid)child["_partyid_value"];
                            string name = (string)child["_partyid_value@OData.Community.Display.V1.FormattedValue"];
                            string logicalName = (string)child["_partyid_value@Microsoft.Dynamics.CRM.lookuplogicalname"];
                            string json = $"{{\"PartyId\":\"{partyId}\",\"Name\":\"{name}\",\"Type\":\"{logicalName}\"}}";
                            records.Add(json);
                        }
                        
                    }
                }
            }

            if(records.Count > 0) {
                result = "[" + string.Join(",", records) +"]";
            }

            return result;
        }

        public override FetchBatch RetrieveAllByFetch(string fetchXml, string entitySetName, List<CrmAttribute> attributes, int pageNumber = 1, int count = 5000, string cookie = null) {
            XmlDocument fetchXmlDoc = new XmlDocument();
            fetchXmlDoc.LoadXml(fetchXml);
            XmlElement root = fetchXmlDoc.DocumentElement;
            root.SetAttribute("page", pageNumber.ToString());
            root.SetAttribute("count", count.ToString());
            if (cookie != null) {
                root.SetAttribute("paging-cookie", cookie);
            }
            fetchXml = fetchXmlDoc.InnerXml;

            List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
            headers.AddRange(new[] {
                new KeyValuePair<string, string>("Accept", "application/json;odata.metadata=minimal"),
                new KeyValuePair<string, string>("Accept-Charset", "UTF-8"),
                new KeyValuePair<string, string>("Content-Type", "multipart/mixed;boundary=batch_divider"),
                new KeyValuePair<string, string>("MSCRMCallerID", "00000000-0000-0000-0000-000000000000"),
                new KeyValuePair<string, string>("OData-MaxVersion", "4.0"),
                new KeyValuePair<string, string>("OData-Version", "4.0"),
                new KeyValuePair<string, string>("Cache-Control", "no-cache"),
            });


            string body =
                $@"--batch_divider
Content-Type: application/http
Content-Transfer-Encoding:binary

GET {helper.connection.ServerUrl}/api/data/v{helper.connection.Organization.OrganizationVersion.ToString("0.0", NumberFormat.NFI.nfi)}/{entitySetName}?fetchXml={WebUtility.UrlEncode(fetchXml)} HTTP/1.1
Content-Type: application/json
OData-Version: 4.0
OData-MaxVersion: 4.0
Prefer: odata.include-annotations=*


--batch_divider--";

            RestHttpResponse response = helper.GetAuthRequest("$batch", "POST", body, headers);
            if(response.ResponseCode != 200) {
                throw new Exception($"Error in FetchXML retrieving for entity {entitySetName}. Http code: {response.ResponseCode}. Message: {response.ResponseString}");
            }
            int firstIndex = response.ResponseString.IndexOf("{");
            int lastIndex = response.ResponseString.LastIndexOf("}");
            JObject jsonResponse = JObject.Parse(response.ResponseString.Substring(firstIndex, lastIndex - firstIndex + 1));
            JToken v = jsonResponse["value"];
            List<JToken> records = v.Children().ToList();
            string nextCookie = (string)jsonResponse["@Microsoft.Dynamics.CRM.fetchxmlpagingcookie"];

            FetchBatch batch = new FetchBatch();
            batch.MoreRecords = !string.IsNullOrEmpty(nextCookie);
            if (!string.IsNullOrEmpty(nextCookie)) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(nextCookie);
                string encoded = doc.FirstChild.Attributes["pagingcookie"].Value;
                batch.PagingCookie = WebUtility.UrlDecode(encoded);
                batch.PagingCookie = WebUtility.UrlDecode(batch.PagingCookie);
            }

            foreach (JToken record in records) {
                CrmRow row = new CrmRow();
                foreach (CrmAttribute crmAttribute in attributes.Where(n => string.IsNullOrEmpty(n.AttributeOf))) {
                    CrmColumn col = new CrmColumn { CrmType = crmAttribute.CrmAttributeType, LogicalName = crmAttribute.LogicalName, Value = null };
                    var valueToken = crmAttribute.LogicalName;
                    col.Value = ConvertJsonToCsharpObject(record, crmAttribute);
                    row.Columns.Add(col);
                }

                foreach (CrmAttribute crmAttribute in attributes.Where(n => !string.IsNullOrEmpty(n.AttributeOf))) {
                    string originalAttributeName = string.IsNullOrEmpty(crmAttribute.Alias) ? crmAttribute.AttributeOf :
                        (crmAttribute.Alias + "_" + crmAttribute.NotAliasedName == crmAttribute.LogicalName ? 
                            crmAttribute.Alias : crmAttribute.Alias + "." + crmAttribute.AttributeOf);

                    CrmAttribute originalAttribute = attributes.FirstOrDefault(n => n.LogicalName == originalAttributeName);
                    if (originalAttribute != null) {
                        //ne radi tg_lookupName
                        CrmColumn col = new CrmColumn { CrmType = crmAttribute.CrmAttributeType, LogicalName = crmAttribute.LogicalName, Value = null };
                        var valueToken = originalAttribute.LogicalName;
                        bool exist = false;
                        if (record[valueToken] != null) {
                            exist = true;
                        }
                        else if(originalAttribute.CrmAttributeType == AttributeTypeEnum.Customer ||
                                originalAttribute.CrmAttributeType == AttributeTypeEnum.Lookup || originalAttribute.CrmAttributeType == AttributeTypeEnum.Owner
                                || originalAttribute.CrmAttributeType == AttributeTypeEnum.Uniqueidentifier) {
                            valueToken = $"_{valueToken}_value";
                            if(record[valueToken] != null) {
                                exist = true;
                            }
                        }

                        if(exist) {
                            string columnName;
                            if (crmAttribute.CrmAttributeType == AttributeTypeEnum.EntityName) {
                                columnName = valueToken + "@Microsoft.Dynamics.CRM.lookuplogicalname";
                            }
                            else {
                                columnName = valueToken + "@OData.Community.Display.V1.FormattedValue";
                            }
                            col.Value = (string)record[columnName];
                        }

                        row.Columns.Add(col);
                    }
                }
                batch.Rows.Add(row);
            }

            return batch;
        }

        object ConvertJsonToCsharpObject(JToken record, CrmAttribute attribute) {
            object value = null;
            string valueToken = attribute.LogicalName;
            switch (attribute.CrmAttributeType) {
                case AttributeTypeEnum.Picklist:
                case AttributeTypeEnum.Status:
                case AttributeTypeEnum.State:
                case AttributeTypeEnum.Integer:
                    value = (int?)record[valueToken];
                    break;
                case AttributeTypeEnum.MultiSelectPicklist:
                    value = (string)record[valueToken];
                    break;
                case AttributeTypeEnum.Lookup:
                case AttributeTypeEnum.Customer:
                case AttributeTypeEnum.Owner:
                case AttributeTypeEnum.Uniqueidentifier:
                    string lookupToken = $"_{valueToken}_value";
                    value = (Guid?)record[lookupToken] ?? (Guid?)record[valueToken];
                    break;
                case AttributeTypeEnum.Money:
                case AttributeTypeEnum.Decimal:
                    value = (decimal?)record[valueToken];
                    break;
                case AttributeTypeEnum.PartyList:
                    value = ExtractParties(record, attribute);
                    break;
                case AttributeTypeEnum.Boolean:
                    value = (bool?)record[valueToken];
                    break;
                case AttributeTypeEnum.DateTime:
                    value = (DateTime?)record[valueToken];
                    break;
                case AttributeTypeEnum.Double:
                    value = (double?)record[valueToken];
                    break;
                case AttributeTypeEnum.BigInt:
                    value = (long?)record[valueToken];
                    break;
                default:
                    value = (string)record[valueToken];
                    break;
            }

            return value;
        }

        public override void Merge(Entity entity, List<OrganizationRequestModel> requests, int batch = 5, bool continueOnError = true, bool returnResponses = true) {
            for (int i = 0; i < requests.Count; i += batch) {
                int reqNum = 1;
                List<OrganizationRequestModel> requestsSub = requests.Skip(i).Take(batch).ToList();
                string changesets = "";
                string changesetId = "c" + Guid.NewGuid().ToString().Substring(0, 12).Replace("-", "");
                string uri = $"{helper.connection.ServerUrl}/api/data/v";
                uri += $"{helper.connection.Organization.OrganizationVersion.ToString(NumberFormat.NFI.nfi)}";
                uri += "/Merge";
                foreach (OrganizationRequestModel req in requestsSub) {
                    req.Response = new OrganizationResponseModel { Success = true };
                    string changeSet =
                        $@"
--changeset_{changesetId}
Content-Type: application/http
Content-Transfer-Encoding:binary
Content-ID: {reqNum}

POST {uri} HTTP/1.1
Content-Type: application/json;type=entry
If-Match: *

";
                    bool parentingChecks = false;
                    var parentingChecksObject = req.Row.Columns.FirstOrDefault(n => n.LogicalName == "PerformParentingChecks")?.Value;
                    if (parentingChecksObject.GetType() != typeof(bool)) {
                        parentingChecks = parentingChecksObject is int ? Convert.ToBoolean(parentingChecksObject) : bool.Parse(parentingChecksObject.ToString());
                    } else {
                        parentingChecks = (bool)parentingChecksObject;
                    }

                    changeSet +=
$@"{{
    ""Target"": {{
        ""@odata.type"": ""Microsoft.Dynamics.CRM.{entity.LogicalName}"",
        ""{entity.IdName}"": ""{req.Row.Columns.First(n => n.LogicalName == "targetid").Value.ToString().Replace("{", "").Replace("}", "").Replace("\"", "")}""
    }},
    ""Subordinate"": {{
        ""@odata.type"": ""Microsoft.Dynamics.CRM.{entity.LogicalName}"",
        ""{entity.IdName}"": ""{req.Row.Columns.First(n => n.LogicalName == "subordinateid").Value.ToString().Replace("{", "").Replace("}", "").Replace("\"", "")}""
    }},
    ""UpdateContent"": {{
        ""@odata.type"": ""Microsoft.Dynamics.CRM.{entity.LogicalName}"",
        ""{entity.IdName}"": ""{Guid.Empty.ToString().Replace("{", "").Replace("}", "").Replace("\"", "")}""
    }},
    ""PerformParentingChecks"": {parentingChecks.ToString().ToLower()}
}}";
                    changesets += changeSet;

                    req.RequestIndex = reqNum++;
                }

                changesets += $"\r\n--changeset_{changesetId}--";
                string batchId = "b" + Guid.NewGuid().ToString().Substring(0, 8).Replace("-", "");
                string body =
                    $@"
--batch_{batchId}
Content-Type: multipart/mixed;boundary=changeset_{changesetId}
{changesets}

--batch_{batchId}--
";

                List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
                headers.AddRange(new[] {
                    new KeyValuePair<string, string>("Accept", "application/json"),
                    new KeyValuePair<string, string>("Content-Type", $"multipart/mixed;boundary=batch_{batchId}"),
                    new KeyValuePair<string, string>("OData-MaxVersion", "4.0"),
                    new KeyValuePair<string, string>("OData-Version", "4.0"),
                });
                RestHttpResponse response = helper.GetAuthRequest("$batch", "POST", body, headers);
                if (response.ResponseCode != 200) {
                    throw new Exception($"Error with merge. Http code: {response.ResponseCode}. Message: {response.ResponseString}");
                }

                string error = TryParseBatchError(response.ResponseString);
                if (error != null) {
                    requestsSub.ForEach(n => {
                        n.Response.Success = false;
                        n.Response.ErrorMessage = error;
                    });
                    if (!continueOnError) {
                        throw new Exception(error);
                    }
                }


                requestsSub.ForEach(n => n.Response.ResponseIndex = n.RequestIndex.Value);

            }
        }

        public override void OrganizationReq(OperationTypeEnum operationType, ComplexMappingEnum idMapping, List<OrganizationRequestModel> requests, int batch = 5, bool continueOnError = true, bool returnResponses = true, bool ignoreNullValuedFields = false, Relationship relationship = null) {
            for(int i = 0; i < requests.Count; i += batch) {
                int reqNum = 1;
                List<OrganizationRequestModel> requestsSub = requests.Skip(i).Take(batch).ToList();
                string changesets = "";
                string changesetId = "c" + Guid.NewGuid().ToString().Substring(0, 12).Replace("-", "");
                string uri = $"{helper.connection.ServerUrl}/api/data/v";
                uri += $"{helper.connection.Organization.OrganizationVersion.ToString(NumberFormat.NFI.nfi)}";
                foreach(OrganizationRequestModel req in requestsSub) {
                    req.Response = new OrganizationResponseModel {Success = true};
                    string entity = null;
                    string changeSet =
                        $@"
--changeset_{changesetId}
Content-Type: application/http
Content-Transfer-Encoding:binary
Content-ID: {reqNum}

";
                    string id = "";
                    //find record keys
                    if (operationType == OperationTypeEnum.Update || operationType == OperationTypeEnum.Upsert || operationType == OperationTypeEnum.Delete) {
                        if (idMapping == ComplexMappingEnum.AlternateKey) {
                            try {
                                id = AlternateId(req.AlternateKeys);
                            }
                            catch (Exception ex) {
                                req.Response.Success = false;
                                req.Response.ErrorMessage = ex.Message;
                            }

                            if (!req.Response.Success) {
                                if (continueOnError) {
                                    continue;
                                }
                                else {
                                    throw new Exception(req.Response.ErrorMessage);
                                }
                            }
                        }
                        else {
                            id = req.PrimaryId.Value.ToString();
                        }
                    }
                    //compose entity
                    if(operationType == OperationTypeEnum.Create || operationType == OperationTypeEnum.Update || operationType == OperationTypeEnum.Upsert) {
                        try {
                            entity = ComposeEntity(req, ignoreNullValuedFields);
                        }
                        catch (Exception ex) {
                            req.Response.Success = false;
                            req.Response.ErrorMessage = ex.Message;
                        }
                        if (!req.Response.Success) {
                            if (continueOnError) {
                                continue;
                            }
                            throw new Exception(req.Response.ErrorMessage);
                        }

                        if(entity == null) {
                            req.Response.Success = false;
                        }
                    }

                    //different operations
                    if(operationType == OperationTypeEnum.Delete) {
                        changeSet +=
$@"DELETE {uri}/{req.Entity.EntitySetName}({id}) HTTP/1.1
Content-Type: application/json;type=entry
MSCRM.SuppressDuplicateDetection: True

";
                        changesets += changeSet;
                    }
                    else if(operationType == OperationTypeEnum.Create) {
                        changeSet += 
$@"POST {uri}/{req.Entity.EntitySetName} HTTP/1.1
Content-Type: application/json;type=entry
MSCRM.SuppressDuplicateDetection: True

";
                        changeSet += entity;
                        changesets += changeSet;
                    }
                    else if(operationType == OperationTypeEnum.Upsert) {
                        changeSet +=
$@"PATCH {uri}/{req.Entity.EntitySetName}({id})?$select={req.Entity.IdName} HTTP/1.1
Content-Type: application/json;type=entry
MSCRM.SuppressDuplicateDetection: True
AutoDisassociate: true
Prefer: return=representation

";
                        changeSet += entity;
                        changesets += changeSet;
                    }
                    else if(operationType == OperationTypeEnum.Update) {
                        changeSet +=
$@"PATCH {uri}/{req.Entity.EntitySetName}({id}) HTTP/1.1
Content-Type: application/json;type=entry
MSCRM.SuppressDuplicateDetection: True
AutoDisassociate: true
If-Match: *

";
                        changeSet += entity;
                        changesets += changeSet;
                    }
                    else if(operationType == OperationTypeEnum.Associate) {
                        CrmColumn c1 = req.Row.Columns.First(n => n.LogicalName == relationship.Entity1AttributeName);
                        CrmColumn c2 = req.Row.Columns.First(n => n.LogicalName == relationship.Entity2AttributeName);
                        string id1 = c1.ComplexMapping == ComplexMappingEnum.AlternateKey ? AlternateId(c1.AlternateKeys) : c1.Value.ToString();
                        string id2 = c2.ComplexMapping == ComplexMappingEnum.AlternateKey ? AlternateId(c2.AlternateKeys) : c2.Value.ToString();
                        id1 = id1.Replace("{", "").Replace("}", "");
                        id2 = id2.Replace("{", "").Replace("}", "");

                        changeSet +=
$@"POST {uri}/{relationship.Entity1EntitySetName}({id1})/{relationship.Name}/$ref HTTP/1.1
Content-Type: application/json;type=entry

";
                        changeSet +=
$@"{{
    ""@odata.id"": ""{uri}/{relationship.Entity2EntitySetName}({id2})""
}}";
                        changesets += changeSet;
                    }
                    else if(operationType == OperationTypeEnum.Disassociate) {
                        CrmColumn c1 = req.Row.Columns.First(n => n.LogicalName == relationship.Entity1AttributeName);
                        CrmColumn c2 = req.Row.Columns.First(n => n.LogicalName == relationship.Entity2AttributeName);
                        string id1 = c1.ComplexMapping == ComplexMappingEnum.AlternateKey ? AlternateId(c1.AlternateKeys) : c1.Value.ToString();
                        string id2 = c2.ComplexMapping == ComplexMappingEnum.AlternateKey ? AlternateId(c2.AlternateKeys) : c2.Value.ToString();
                        id1 = id1.Replace("{", "").Replace("}", "");
                        id2 = id2.Replace("{", "").Replace("}", "");
                        changeSet +=
$@"DELETE {uri}/{relationship.Entity1EntitySetName}({id1})/{relationship.Name}({id2})/$ref HTTP/1.1
Content-Type: application/json;type=entry

";
                        changesets += changeSet;
                    }

                    req.RequestIndex = reqNum++;
                }

                changesets += $"\r\n--changeset_{changesetId}--";
                string batchId = "b" + Guid.NewGuid().ToString().Substring(0, 8).Replace("-", "");
                string body =
                    $@"
--batch_{batchId}
Content-Type: multipart/mixed;boundary=changeset_{changesetId}
{changesets}

--batch_{batchId}--
";
                
                List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
                headers.AddRange(new[] {
                    new KeyValuePair<string, string>("Accept", "application/json"),
                    new KeyValuePair<string, string>("Content-Type", $"multipart/mixed;boundary=batch_{batchId}"),
                    new KeyValuePair<string, string>("OData-MaxVersion", "4.0"),
                    new KeyValuePair<string, string>("OData-Version", "4.0"),
                });
                RestHttpResponse response = helper.GetAuthRequest("$batch", "POST", body, headers);
                if(response.ResponseCode != 200) {
                    throw new Exception($"Error with {operationType}. Http code: {response.ResponseCode}. Message: {response.ResponseString}");
                }

                string error = TryParseBatchError(response.ResponseString);
                if(error != null) {
                    requestsSub.ForEach(n => {
                        n.Response.Success = false;
                        n.Response.ErrorMessage = error;
                    });
                    if (!continueOnError)                     {
                        throw new Exception(error);
                    }
                }

                if(operationType == OperationTypeEnum.Create || operationType == OperationTypeEnum.Upsert) {
                    List<string> responses = response.ResponseString.Split(new[] { "Content-ID" }, StringSplitOptions.None).Where(n => n.Contains("HTTP/")).ToList();
                    Regex regex1 = new Regex(@"\d+");
                    if(operationType == OperationTypeEnum.Create) {
                        Regex regex2 = new Regex(@"[(].+[)]");
                        foreach(string r in responses) {
                            MatchCollection matches1 = regex1.Matches(r);
                            int respNum = int.Parse(matches1[0].Value);
                            var req = requestsSub.First(n => n.RequestIndex == respNum);
                            req.Response.ResponseIndex = respNum;
                            string idPart = r.Substring(r.IndexOf("OData-EntityId"));
                            MatchCollection matches2 = regex2.Matches(idPart);
                            string guidString = matches2[0].Value.Replace("{", "").Replace("}", "");
                            req.Response.RecordCreated = true;
                            req.Response.Id = new Guid(guidString);
                        }
                    } else if(operationType == OperationTypeEnum.Upsert) {
                        Regex regex2 = new Regex(@"[{].+[}]");
                        foreach(string r in responses) {
                            MatchCollection matches1 = regex1.Matches(r);
                            int respNum = int.Parse(matches1[0].Value);
                            var req = requestsSub.First(n => n.RequestIndex == respNum);
                            req.Response.ResponseIndex = respNum;

                            MatchCollection matches2 = regex2.Matches(r);
                            if(matches2.Count > 0) {
                                JObject jsonResponse = JObject.Parse(matches2[0].Value);
                                string id = jsonResponse[req.Entity.IdName].ToString();
                                req.Response.Id = new Guid(id);
                            }
                        }
                    }
                }
                else if(operationType == OperationTypeEnum.Delete || operationType == OperationTypeEnum.Update) {
                    requestsSub.ForEach(n => n.Response.ResponseIndex = n.RequestIndex.Value);
                }
            }
        }

        private string TryParseBatchError(string response) {
            //error handling
            string result = null;
            int firstIndex = response.IndexOf("{");
            int lastIndex = response.LastIndexOf("}");
            if (firstIndex >= 0 && lastIndex >= 0) {
                try {
                    JObject jsonResponse = JObject.Parse(response.Substring(firstIndex, lastIndex - firstIndex + 1));
                    JToken error = jsonResponse["error"];
                    if (error != null) {
                        result = error["message"].ToString();
                    }
                }
                catch (Newtonsoft.Json.JsonReaderException ex) { }
                catch (Exception ex) {
                    throw ex;
                }
            }//end of error handling

            return result;
        }

        private string ComposeEntity(OrganizationRequestModel req, bool ignoreNullValuedFields) {
            string entity = @"{
";
            List<string> lineList = new List<string>();
            foreach(CrmColumn column in req.Row.Columns) {
                if (column.CrmType == AttributeTypeEnum.Lookup || column.CrmType == AttributeTypeEnum.Customer || column.CrmType == AttributeTypeEnum.Owner) {
                    string line;
                    if (column.ComplexMapping == ComplexMappingEnum.AlternateKey) {
                        if(column.AlternateKeys.All(n => n.Value == null)) {
                            if(ignoreNullValuedFields) {
                                continue;
                            }
                            line = $"    \"_{column.LogicalName}_value\": null";
                        } else {
                            string id = AlternateId(column.AlternateKeys);
                            line = $"    \"{column.LookupTarget.LookupOdataName}@odata.bind\": \"/{column.LookupTarget.TargetEntitySetName}({id})\"";
                        }
                    } else {//primarid, manual
                        if(column.Value == null) {
                            if(ignoreNullValuedFields) {
                                continue;
                            }
                            line = $"    \"_{column.LogicalName}_value\": null";
                        } else {
                            string id = column.Value.ToString().Replace("{", "").Replace("}", "");
                            line = $"    \"{column.LookupTarget.LookupOdataName}@odata.bind\": \"/{column.LookupTarget.TargetEntitySetName}({id})\"";
                        }
                    }
                    lineList.Add(line);
                } else {//all other data types
                    if(ignoreNullValuedFields && column.Value == null) {
                        continue;
                    }

                    string line = $"    \"{column.LogicalName}\": ";
                    object value = column.Value;
                    if(column.Value == null) {
                        line += "null";
                    } else {
                        switch(column.CrmType) {
                            case AttributeTypeEnum.Picklist:
                            case AttributeTypeEnum.Status:
                            case AttributeTypeEnum.State:
                                int? optionValue = AttributeHelpers.SetOptionsetValue(column);
                                if (optionValue == null) {
                                    throw new Exception("Can't find optionset value for " + column.Value.ToString());
                                }

                                line += optionValue.Value.ToString();
                                break;
                            case AttributeTypeEnum.BigInt:
                                if(column.Value.GetType() != typeof(long)) {
                                    value = long.Parse(column.Value.ToString());
                                }

                                line += ((long)value).ToString();
                                break;
                            case AttributeTypeEnum.Boolean:
                                if(column.Value.GetType() != typeof(bool)) {
                                    value = column.Value is int ? Convert.ToBoolean(column.Value) : bool.Parse(column.Value.ToString());
                                }
                                line += (bool)value ? "true" : "false";
                                break;
                            case AttributeTypeEnum.DateTime:
                                if(column.Value.GetType() != typeof(DateTime)) {
                                    value = DateTime.Parse(column.Value.ToString());
                                }
                                line += "\"" + ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "\"";
                                break;
                            case AttributeTypeEnum.Decimal:
                            case AttributeTypeEnum.Money:
                                if(column.Value.GetType() != typeof(decimal)) {
                                    value = decimal.Parse(column.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                                }

                                line += ((decimal)value).ToString(NumberFormat.NFI.nfi);
                                break;
                            case AttributeTypeEnum.Double:
                                if(column.Value.GetType() != typeof(double)) {
                                    value = double.Parse(column.Value.ToString().Replace(",", "."), NumberFormat.NFI.nfi);
                                }

                                line += ((double)value).ToString(NumberFormat.NFI.nfi);
                                break;
                            case AttributeTypeEnum.Integer:
                                if(column.Value.GetType() != typeof(int)) {
                                    value = int.Parse(column.Value.ToString());
                                }
                                line += ((int)value).ToString();
                                break;
                            case AttributeTypeEnum.Memo:
                            case AttributeTypeEnum.String:
                            case AttributeTypeEnum.MultiSelectPicklist:
                                if(column.Value.GetType() != typeof(string)) {
                                    value = value.ToString();
                                }
                                line += "\"" + value.ToString().Replace("\"", "\\\"") + "\"";
                                break;
                            case AttributeTypeEnum.Uniqueidentifier:
                                if (column.Value.GetType() != typeof(Guid)) {
                                    value = value.ToString();
                                }
                                line += "\"" + value.ToString().Replace("{", "").Replace("}", "").Replace("\"", "") + "\"";
                                break;
                        }
                        //line += value;
                    }

                    lineList.Add(line);
                }
            }

            if(lineList.Count > 0) {
                entity += String.Join(",\r\n", lineList);
                entity += @"
}";
            } else {
                entity = null;
            }
            return entity;
        }

        private string AlternateId(List<CrmColumn> alternateKeys) {
            List<string> keys = new List<string>();
            foreach (CrmColumn alternateKey in alternateKeys) {
                string k = null;
                if (alternateKey.Value == null || alternateKey.Value == DBNull.Value) {
                    string errorMessage = $"Value of alternate key column can't be null. "
                                          + string.Join(", ", alternateKeys.Select(n => n.LogicalName + ": " + n.Value.ToString()));
                    throw new AlternateKeyException(errorMessage);
                }

                if (alternateKey.CrmType == AttributeTypeEnum.String) {
                    k = alternateKey.LogicalName + $"='{alternateKey.Value}'";
                }
                else if (alternateKey.CrmType == AttributeTypeEnum.DateTime) {
                    string v1 = null;
                    if (alternateKey.Value is DateTime v) {
                        v1 = v.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    }
                    k = alternateKey.LogicalName + "=" + v1;
                }
                else if (alternateKey.CrmType == AttributeTypeEnum.Lookup) {
                    k = $"_{alternateKey.LogicalName}_value={((Guid?)alternateKey.Value).Value.ToString().Replace("{", "").Replace("}", "")}";
                }
                else if (alternateKey.CrmType == AttributeTypeEnum.Decimal) {
                    k = alternateKey.LogicalName + "=" + ((Decimal?)alternateKey.Value).Value.ToString(NumberFormat.NFI.nfi);
                }
                else {
                    k = alternateKey.LogicalName + "=" + alternateKey.Value;
                }
                keys.Add(k);
            }

            return string.Join(",", keys);
        }

        public override void FindRecordIdByMatching(List<AttributeMatchingRequestModel> requests, int batch = 10) {
            Dictionary<string, List<AttributeMatchingRequestModel>> groups = new Dictionary<string, List<AttributeMatchingRequestModel>>();
            foreach (AttributeMatchingRequestModel request in requests) {
                string key = request.Entity.LogicalName + "/" + string.Join("|", request.Row.Columns.Select(n => n.CrmAttribute.LogicalName));
                if(groups.ContainsKey(key)) {
                    groups[key].Add(request);
                } else {
                    groups.Add(key, new List<AttributeMatchingRequestModel>(){request});
                }
            }

            foreach(KeyValuePair<string, List<AttributeMatchingRequestModel>> grp in groups) {
                List<AttributeMatchingRequestModel> grpRequests = grp.Value;
                List<CrmAttribute> matchingColumns = grpRequests[0].Row.Columns.Select(n => n.CrmAttribute).OrderBy(n => n.LogicalName).ToList();
                for (int i = 0; i < grpRequests.Count; i += batch) {
                    int reqNum = 0;
                    string batchId = "b" + Guid.NewGuid().ToString().Substring(0, 12).Replace("-", "");
                    List<AttributeMatchingRequestModel> requestsSub = grpRequests.Skip(i).Take(batch).ToList();
                    string batchBody = @"
";
                    var entity = grpRequests[0].Entity;
                    foreach (AttributeMatchingRequestModel req in requestsSub) {
                        //fetch xml
                        
                        
                        string uri = $"{helper.connection.ServerUrl}/api/data/v";
                        uri += $"{helper.connection.Organization.OrganizationVersion.ToString(NumberFormat.NFI.nfi)}/{entity.EntitySetName}?fetchXml=";
                        XmlDocument fetchXml = new XmlDocument();
                        var root_f = fetchXml.CreateElement("fetch");
                        fetchXml.AppendChild(root_f);
                        var entity_f = fetchXml.CreateElement("entity");
                        entity_f.SetAttribute("name", entity.LogicalName);
                        root_f.AppendChild(entity_f);

                        var id_f = fetchXml.CreateElement("attribute");
                        id_f.SetAttribute("name", entity.IdName);
                        entity_f.AppendChild(id_f);
                        var filter_f = fetchXml.CreateElement("filter");
                        filter_f.SetAttribute("type", "and");
                        entity_f.AppendChild(filter_f);
                        List<XmlElement> filterElements = new List<XmlElement>();
                        foreach(CrmAttribute attr in matchingColumns) {
                            var attribute_f = fetchXml.CreateElement("attribute");
                            attribute_f.SetAttribute("name", attr.LogicalName);
                            entity_f.AppendChild(attribute_f);
                            var condition_f = fetchXml.CreateElement("condition");
                            condition_f.SetAttribute("attribute", attr.LogicalName);
                            condition_f.SetAttribute("operator", "eq");
                            filterElements.Add(condition_f);
                            filter_f.AppendChild(condition_f);
                        }
                        //fetch xml

                        foreach(CrmAttribute attr in matchingColumns) {
                            string stringValue = null;
                            CrmColumn col = req.Row.Columns.First(n => n.LogicalName == attr.LogicalName);
                            stringValue = AttributeHelpers.ColumnToString(col);
                            var cond = filterElements.First(n => n.Attributes["attribute"].Value == attr.LogicalName);
                            if(attr.CrmAttributeType == AttributeTypeEnum.MultiSelectPicklist) {
                                cond.IsEmpty = true;
                                if(string.IsNullOrEmpty(stringValue)) {
                                    cond.SetAttribute("operator", "null");
                                } else {
                                    cond.SetAttribute("operator", "in");
                                    string[] values = stringValue.Split(',', ' ');
                                    foreach(string v in values) {
                                        try {
                                            int vn = int.Parse(v);
                                        } catch(Exception ex) {
                                            string errorMessage = $"Error converting multiselect option set string value {v} to integer. " + ex.Message;
                                            throw new Exception(errorMessage, ex.InnerException);
                                        }

                                        var value_f = fetchXml.CreateElement("value");
                                        value_f.InnerText = v.Trim();
                                        cond.AppendChild(value_f);
                                    }
                                }
                            } else if(string.IsNullOrEmpty(stringValue)) {
                                cond.SetAttribute("operator", "null");
                            } else {
                                cond.SetAttribute("operator", "eq");
                                cond.SetAttribute("value", stringValue);
                            }

                            stringValue = string.IsNullOrEmpty(stringValue) ? "" : stringValue;
                        }

                        batchBody +=
                            $@"--batch_{batchId}
Content-Type: application/http
Content-Transfer-Encoding: binary

GET {uri}{WebUtility.UrlEncode(fetchXml.InnerXml)} HTTP/1.1
Accept: application/json
Prefer: odata.include-annotations=""*"",odata.maxpagesize={Parameters.UpdateAllCount}

";
                    }

                    batchBody +=
                        $@"--batch_{batchId}--
";
                    List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
                    headers.AddRange(new[] {
                        new KeyValuePair<string, string>("Accept", "application/json"),
                        new KeyValuePair<string, string>("Content-Type", $"multipart/mixed;boundary=batch_{batchId}"),
                        new KeyValuePair<string, string>("OData-MaxVersion", "4.0"),
                        new KeyValuePair<string, string>("OData-Version", "4.0"),
                    });
                    RestHttpResponse response = helper.GetAuthRequest("$batch", "POST", batchBody, headers);
                    if(response.ResponseCode != 200) {
                        throw new Exception($"Error finding lookups for {entity.LogicalName}. Http code: {response.ResponseCode}. Message: {response.ResponseString}");
                    }

                    string error = TryParseBatchError(response.ResponseString);
                    if(error != null) {
                        throw new Exception($"Error finding error by ID matching. {error}");
                    }

                    List<string> parts = response.ResponseString.Split(new[] { "--batchresponse" }, StringSplitOptions.None).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    List<JObject> results = new List<JObject>();
                    Regex regex = new Regex(@"[{].+[}]");
                    foreach(string part in parts) {
                        MatchCollection matches = regex.Matches(part);
                        if(matches.Count > 0) {
                            results.Add(JObject.Parse(matches[0].Value));
                        }
                    }

                    foreach(JObject result in results) {
                        JToken v = result["value"];
                        var x6 = v.ToString();
                        List<JToken> records = v.Children().ToList();
                        string matchingString = "";
                        if(records.Count > 0) {
                            foreach(CrmAttribute attr in matchingColumns) {
                                CrmColumn col = new CrmColumn { CrmType = attr.CrmAttributeType, LogicalName = attr.LogicalName, Value = null };
                                col.Value = ConvertJsonToCsharpObject(records[0], attr);
                                string matching = "";
                                if(col.Value != null) {
                                    switch(attr.CrmAttributeType) {
                                        case AttributeTypeEnum.Decimal:
                                        case AttributeTypeEnum.Money:
                                            matching = ((decimal)col.Value).ToString(NumberFormat.NFI.nfi);
                                            break;
                                        case AttributeTypeEnum.Double:
                                            matching = ((double)col.Value).ToString(NumberFormat.NFI.nfi);
                                            break; //kavran
                                        case AttributeTypeEnum.DateTime:
                                            matching = ((DateTime)col.Value).ToString("yyyy-MM-dd HH:mm.ss");
                                            break;
                                        default:
                                            matching = col.Value.ToString();
                                            break;
                                    }

                                    matchingString = AttributeHelpers.AddColumnValueToUniqueString(matchingString, attr.LogicalName, matching);
                                }
                            }
                        }

                        List<AttributeMatchingRequestModel> founded = grpRequests.Where(n => n.AttributeMatchUniqueString == matchingString).ToList();
                        OrganizationResponseModel responseModel = new OrganizationResponseModel();
                        responseModel.Success = true;
                        responseModel.ComputedGuids = new List<Guid>();
                        if(founded.Count > 0) {
                            List<Guid> ids = records.Select(n => (Guid)n[founded[0].Entity.IdName]).ToList();
                            responseModel.ComputedGuids = ids;
                        }

                        founded.ForEach(n => n.Response = responseModel);
                    }
                }
            }

            OrganizationResponseModel notFound = new OrganizationResponseModel() {
                Success = true,
                ComputedGuids = new List<Guid>()
            };
            requests.Where(n => n.Response == null).ToList().ForEach(n => n.Response = notFound);
            
        }

        public override bool CheckOrganizationUri() {
            throw new NotImplementedException();
        }
    }
}
