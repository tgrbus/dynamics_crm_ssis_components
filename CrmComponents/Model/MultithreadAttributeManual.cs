using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace CrmComponents.Model
{
    public class MultithreadAttributeManual {
        private readonly object lockObject = new object();
        private List<AttributeMatchingRequestModel> allRequests;
        private List<AttributeMatchingRequestModel> uniqueList = new List<AttributeMatchingRequestModel>();
        private List<AttributeMatchingRequestModel> emptyValues = new List<AttributeMatchingRequestModel>();
        private int nextPointer = 0;
        private int currentLast = 0;
        private int noOfRows;
        private int batchSize;
        private ErrorHandlingEnum errorHandling;
        private IDTSComponentMetaData100 metadata;
        private const string MyLogEntryName = "My Custom Destination Log Entry";
        private string connectionString;
        private List<string> errors = new List<string>();

        public MultithreadAttributeManual(string connectionString, List<AttributeMatchingRequestModel> requests, int batchSize, ErrorHandlingEnum errorHandling) {
            this.connectionString = connectionString;
            this.allRequests = requests;
            this.batchSize = batchSize;
            this.errorHandling = errorHandling;
            FindDuplicates();
        }

        private void FindDuplicates() {
            int counter = 1;
            emptyValues = allRequests.Where(n => n.Row.Columns.All(k => k.Value == null)).ToList();
            if(emptyValues.Count > 0) {
                var firstOnError = emptyValues.FirstOrDefault(n => n.AttributeMatchNotFound == MatchingEnum.RaiseError);
                if (firstOnError != null) {
                    string errorMessage = $"Null or empty values for lookup {firstOnError.OriginalColumnForAttributeLookup.LogicalName}";
                    throw new Exception(errorMessage);
                }

                foreach(AttributeMatchingRequestModel emptyValue in emptyValues) {
                    var defaultValue = emptyValue.AttributeMatchDefaultValue;
                    defaultValue = defaultValue == "null" || string.IsNullOrEmpty(defaultValue) ? null : defaultValue;
                    emptyValue.OriginalColumnForAttributeLookup.Value = defaultValue == null ? (Guid?)null : Guid.Parse(defaultValue);
                }
            }
            foreach (AttributeMatchingRequestModel request in allRequests.Where(n => n.Row.Columns.Any(k => k.Value != null))) {
                if(!uniqueList.Exists(n => n.AttributeMatchUniqueString == request.AttributeMatchUniqueString)) {
                    request.AttributeMatchUniqueIndex = counter++;
                    uniqueList.Add(request);
                }
            }

            this.noOfRows = uniqueList.Count;
        }

        public void Execute(int numOfThreads) {
            string errorLine = "Begging 47";
            try {
                List<Task> tasks = new List<Task>();

                for(int i = 0; i < numOfThreads; i++) {
                    var i1 = i;
                    var task = new Task(() => FindGuids($"Task no. {i1 + 1}"));
                    tasks.Add(task);
                    task.Start();
                }

                try {
                    Task.WaitAll(tasks.ToArray());
                } catch(Exception ex) {
                    throw new Exception(ex + ex.InnerException.Message);
                }

                Task faulted = tasks.FirstOrDefault(n => n.IsFaulted);
                if(faulted != null) {
                    throw faulted.Exception?.InnerExceptions?[0];
                }

                if(errorHandling == ErrorHandlingEnum.Fail) {
                    var failed = uniqueList.FirstOrDefault(n => n.Response?.Success == false);
                    if(failed != null) {
                        throw new Exception(failed.Response?.ErrorMessage);
                    }
                }

                foreach (AttributeMatchingRequestModel request in uniqueList) {
                    List<AttributeMatchingRequestModel> same = allRequests.Where(n => n.AttributeMatchUniqueString == request.AttributeMatchUniqueString).ToList();
                    foreach(AttributeMatchingRequestModel requestModel in same) {
                        requestModel.OriginalColumnForAttributeLookup.ComplexMapping = ComplexMappingEnum.PrimaryKey;
                        requestModel.FoundedIds = request.Response?.ComputedGuids;
                    }
                }

                errorLine = "After foreach(AttributeMatchingRequestModel request in uniqueList) 81";
                int errorCount = 0;
                string errorMessage = "";
                foreach(AttributeMatchingRequestModel request in allRequests) {
                    if(request.FoundedIds == null || request.FoundedIds.Count == 0) {
                        errorLine = "86";
                        if (request.AttributeMatchNotFound == MatchingEnum.DefaultValue) {
                            Guid? value = null;
                            errorLine = "line 89";
                            if (!string.IsNullOrEmpty(request.AttributeMatchDefaultValue)) {
                                if (request.AttributeMatchDefaultValue.ToLower().Trim() == "null") {
                                    value = null;
                                }
                                else {
                                    value = Guid.Parse(request.AttributeMatchDefaultValue);
                                }
                            }

                            errorLine = "After default value 97";
                            request.OriginalColumnForAttributeLookup.Value = value;
                        }
                        else if (request.AttributeMatchNotFound == MatchingEnum.RaiseError) {
                            if (++errorCount >= 5) {
                                throw new Exception(errorMessage);
                            }

                            errorMessage += $"Can't find {request.Entity} record for values ({string.Join(",", request.Row.Columns.Select(n => n.LogicalName))})";
                            errorMessage += $" = ({string.Join(",", request.Row.Columns.Select(n => n.Value.ToString()))})\r\n";
                        }
                    }
                    else if (request.FoundedIds.Count == 1) {
                        errorLine = "112";
                        request.OriginalColumnForAttributeLookup.Value = request.FoundedIds[0];
                    } else if(request.FoundedIds.Count > 1) {
                        errorLine = "116";
                        if(request.AttributeMatchMultiple == MatchingEnum.MatchOne) {
                            request.OriginalColumnForAttributeLookup.Value = request.FoundedIds.First<Guid>();
                        } else if(request.AttributeMatchMultiple == MatchingEnum.RaiseError) {
                            if(++errorCount >= 5) {
                                throw new Exception(errorMessage);
                            }

                            errorMessage += $"Multiple record found for {request.Entity} for values ({string.Join(",", request.Row.Columns.Select(n => n.LogicalName))})";
                            errorMessage += $" = ({string.Join(",", request.Row.Columns.Select(n => n.Value.ToString()))})\r\n";
                        }
                    }
                }

                if(errorCount > 0) {
                    throw new Exception(errorMessage);
                }
            } catch(Exception ex) {
                throw new Exception(errorLine + "/" + ex.Message + "/" + ex.StackTrace);
            }
        }

        private void FindGuids(string name) {
            Thread.CurrentThread.Name = name;
            CrmCommands commands = Connection.CrmCommandsFactory(connectionString);
            int first = -1;
            while(first < noOfRows) {
                lock(lockObject) {
                    first = nextPointer;
                    nextPointer += batchSize;
                }

                if(first < noOfRows) {
                    List<AttributeMatchingRequestModel> localRows = uniqueList.Skip(first).Take(batchSize).ToList();
                    try {
                        commands.FindRecordIdByMatching(localRows);
                    } catch(Exception ex) {
                        errors.Add(ex.Message + "/" + ex.StackTrace);
                        throw new Exception(ex.Message + "/" + ex.StackTrace);
                    } finally {
                        lock(lockObject) {
                            int last = localRows.Last().AttributeMatchUniqueIndex;
                            currentLast = last > currentLast ? last : currentLast;
                        }
                    }
                }
            }
        }
    }
}
