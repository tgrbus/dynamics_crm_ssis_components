using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CrmComponents.Helpers.Enums;
using CrmComponents.Soap;

namespace CrmComponents.Model
{
    public class FetchXmlHelpers {
        private EntityModel model;
        private List<Entity> entityList = new List<Entity>();
        private CrmCommands commands;

        private CrmAttribute GetAttribute(string entityName, string attributeName) {
            Entity entity = entityList.FirstOrDefault(n => n.LogicalName == entityName);
            Entity fromModel = model.EntityList.First(n => n.LogicalName.ToLower() == entityName.ToLower());
            if (entity == null) {
                var temp = new Entity();
                entity = new Entity {
                    Attributes = commands.RetrieveAttributesList(model, fromModel),
                    LogicalName = entityName
                };
                this.entityList.Add(entity);
            }

            return entity.Attributes.First(n => n.LogicalName == attributeName);            
        }

        private List<CrmAttribute> GetAdditionalAttributes(string entityName, string attributeName) {
            Entity entity = entityList.FirstOrDefault(n => n.LogicalName == entityName);
            Entity fromModel = model.EntityList.First(n => n.LogicalName.ToLower() == entityName.ToLower());
            if (entity == null) {
                entity = new Entity {
                    Attributes = commands.RetrieveAttributesList(model, fromModel),
                    LogicalName = entityName
                };
                this.entityList.Add(entity);
            }
            return entity.Attributes.Where(n => n.AttributeOf == attributeName).ToList();
        }

        private string CheckFetch(string fetchXml) {
            try {
                var x = commands.RetrieveAllByFetch(fetchXml, this.model.SelectedEntity.EntitySetName,new List<CrmAttribute>(), 1, 1, null);
                return null;
            } catch(Exception ex) {
                return ex?.InnerException?.Message + ", " + ex.Message;
            }
        }

        public List<string> GetVariablesNames(string fetchXml) {
            string pattern = @"@.*?\]";
            Regex rgx = new Regex(pattern);
            List<string> result = new List<string>();
            foreach(Match match in rgx.Matches(fetchXml)) {
                string value = match.Value.Substring(2, match.Value.Length - 3);
                if(!result.Exists(n => n == value)) {
                    result.Add(value);
                }
            }

            return result;
        }

        public string SetVariables(string fetchXml, List<SsisVariable> vars) {
            string result = String.Copy(fetchXml);
            string pattern = @"@.*?\]";
            Regex rgx = new Regex(pattern);
            List<KeyValuePair<string, string>> matches = new List<KeyValuePair<string, string>>();
            foreach(Match match in rgx.Matches(result)) {
                string value = match.Value;
                if(!matches.Exists(n => n.Key == value)) {
                    var var = vars.FirstOrDefault(v => v.Name.Contains(value.Substring(2, value.Length - 3)));
                    if(var != null) {
                        matches.Add(new KeyValuePair<string, string>(value, var.ToString()));
                    }
                }
            }

            foreach(KeyValuePair<string, string> pair in matches) {
                result = result.Replace(pair.Key, pair.Value);
            }

            return result;
        }

        public string SetColumns(string fetchXml, EntityModel model, CrmCommands commands) {
            this.model = model;
            this.commands = commands;
            XmlDocument xDoc = new XmlDocument();
            try {
                xDoc.LoadXml(fetchXml);
                var mainEntityName = xDoc.DocumentElement?.FirstChild?.Attributes?["name"].Value;
                model.SelectedEntity = model.EntityList.First(n => n.LogicalName.ToLower() == mainEntityName.ToLower());
            } catch(Exception ex) {
                return ex.Message;
            }

            string error = CheckFetch(fetchXml);
            if(error != null) {
                return error;
            }
            List<CrmAttribute> attributes = new List<CrmAttribute>();

            var isAggregate = xDoc.DocumentElement?.Attributes["aggregate"]?.Value == "true";

            XmlNodeList attributesNodeList = xDoc.GetElementsByTagName("attribute");
            string parentEntity = "";
            foreach(XmlNode node in attributesNodeList) {
                string attributeName = node?.Attributes["name"]?.Value;
                XmlNode parent = node?.ParentNode;
                string entityName = parent.Attributes["name"]?.Value;
                string alias = isAggregate ? node?.Attributes["alias"]?.Value : parent.Attributes["alias"]?.Value ?? "";
                
                if(string.IsNullOrEmpty(alias)) {
                    parentEntity = entityName;
                }
                CrmAttribute a = this.model.Attributes.FirstOrDefault(n => n.NotAliasedName == attributeName && n.Entity == entityName)?.ShallowCopy() 
                                 ?? GetAttribute(entityName, attributeName).ShallowCopy();
                if(isAggregate) {
                    a.LogicalName = alias;
                    string aggregateType = node?.Attributes["aggregate"]?.Value?.ToLower();
                    if(aggregateType == "count" || aggregateType == "countcolumn" || node?.Attributes["dategrouping"] != null) {
                        a.CrmAttributeType = AttributeTypeEnum.Integer;
                        a.Precision = 0;
                        a.Scale = 0;
                    } 
                } else {
                    a.LogicalName = string.IsNullOrEmpty(alias) ? a.NotAliasedName : $"{alias}.{a.NotAliasedName}";
                }
                a.Alias = alias;

                attributes.Add(a);

                List<CrmAttribute> additionalAttributes = GetAdditionalAttributes(entityName, attributeName);
                foreach(CrmAttribute additionalAttribute in additionalAttributes) {
                    CrmAttribute a1 = this.model.Attributes.FirstOrDefault(n => n.NotAliasedName == attributeName && n.Entity == entityName)?.ShallowCopy()
                                      ?? additionalAttribute.ShallowCopy();
                    
                    if (isAggregate) {
                        a1.LogicalName = $"{alias}_{a1.NotAliasedName}";
                        string aggregateType = node?.Attributes["aggregate"]?.Value?.ToLower();
                        if (aggregateType == "count" || aggregateType == "countcolumn" || node?.Attributes["dategrouping"] != null) {
                            a1.CrmAttributeType = AttributeTypeEnum.Integer;
                            a1.Precision = 0;
                            a1.Scale = 0;
                        }
                    } else {
                        a1.LogicalName = string.IsNullOrEmpty(alias) ? a1.NotAliasedName : $"{alias}.{a1.NotAliasedName}";
                    }
                    a1.Alias = alias;
                    attributes.Add(a1);
                }
                
            }

            if(!isAggregate) {
                string idName = entityList.First(n => n.LogicalName == parentEntity).IdName;
                if(idName != null && attributes.FirstOrDefault(n => n.LogicalName == idName) == null) {
                    var a = GetAttribute(parentEntity, idName);
                    a.Alias = a.Alias ?? "";
                    attributes.Add(a);
                }
            }

            attributes = attributes.OrderBy(n => n.Alias).ThenBy(n => n.NotAliasedName).ToList();
            this.model.Attributes = attributes;

            return null;
        }
    }
}
