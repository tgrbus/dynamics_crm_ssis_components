using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;
using CrmComponents.Soap;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace CrmComponents.Model
{
    public class MultiThreadObject {
        private readonly object lockObject = new object();
        private List<OrganizationRequestModel> requests;
        private List<OrganizationRequestModel> uniqueList;
        private int nextPointer = 0;
        private int currentLast = 0;
        private int noOfRows;
        private OperationTypeEnum operationType;
        private ComplexMappingEnum idMapping;
        private int batchSize;
        private ErrorHandlingEnum errorHandling;
        private bool ignoreNullValuedFields = false;
        private IDTSComponentMetaData100 metadata;
        private const string MyLogEntryName = "My Custom Destination Log Entry";
        private string connectionString;
        private List<CrmAttribute> matchingCrmAttributes;
        private MatchingEnum multipleMatches;
        private MatchingEnum matchNotFound;
        private Model.Relationship relationship;
        private string entityName;
        private Entity mergeEntity;

        public MultiThreadObject(string connectionString, OperationTypeEnum operationType, ComplexMappingEnum idMapping, List<OrganizationRequestModel> requests, 
                                 int batchSize, ErrorHandlingEnum errorHandling, IDTSComponentMetaData100 metadata) {
            this.operationType = operationType;
            this.idMapping = idMapping;
            this.requests = requests;
            noOfRows = this.requests.Count;
            this.batchSize = batchSize;
            this.errorHandling = errorHandling;
            this.metadata = metadata;
            this.connectionString = connectionString;
        }

        public MultiThreadObject() {

        }

        public void ExecuteMerge(int numOfThreads, Entity entity) {
            mergeEntity = entity;
            List<Task> tasks = new List<Task>();

            for(int i = 0; i < numOfThreads; i++) {
                var i1 = i;
                var task = new Task(() => Merge($"Task no. {i1 + 1}"));
                tasks.Add(task);
                task.Start();
            }

            while(tasks.Exists(n => n.Status != TaskStatus.RanToCompletion && n.Status != TaskStatus.Faulted)) {
                Task faulted = tasks.FirstOrDefault(n => n.IsFaulted);
                if(faulted != null) {
                    throw faulted.Exception?.InnerExceptions?[0];
                }

                if(errorHandling == ErrorHandlingEnum.Fail) {
                    var failed = requests.FirstOrDefault(n => n.Response?.Success == false);
                    if (failed != null) {
                        throw new Exception(failed.Response?.ErrorMessage);
                    }
                }
                Thread.Sleep(Parameters.ThreadSleepMiliseconds);
            }

            foreach (Task task in tasks) {
                if (task.Status == TaskStatus.Faulted) {
                    throw task.Exception?.InnerExceptions?[0];
                }
            }
        }

        public void ExecuteIntersect(int numOfThreads, Model.Relationship relationship) {
            List<Task> tasks = new List<Task>();
            this.relationship = relationship;

            for(int i = 0; i < numOfThreads; i++) {
                var i1 = i;
                var task = new Task(() => UpsertIntersect($"Task no. {i1 + 1}"));
                tasks.Add(task);
                task.Start();
            }

            while (tasks.Exists(n => n.Status != TaskStatus.RanToCompletion && n.Status != TaskStatus.Faulted)) {
                Task faulted = tasks.FirstOrDefault(n => n.IsFaulted);
                if (faulted != null) {
                    throw faulted.Exception?.InnerExceptions?[0];
                }
                if (errorHandling == ErrorHandlingEnum.Fail) {
                    var failed = requests.FirstOrDefault(n => n.Response?.Success == false);
                    if (failed != null) {
                        throw new Exception(failed.Response?.ErrorMessage);
                    }
                }
                Thread.Sleep(Parameters.ThreadSleepMiliseconds);
            }

            foreach (Task task in tasks) {
                if (task.Status == TaskStatus.Faulted) {
                    throw task.Exception?.InnerExceptions?[0];
                }
            }
        }


        public void Execute(int numOfThreads) {
            try {
                List<Task> tasks = new List<Task>();
                for(var i = 0; i < numOfThreads; i++) {
                    var i1 = i;
                    var task = new Task(() => Update($"Task no. {i1 + 1}"));
                    tasks.Add(task);
                    task.Start();
                }

                while(tasks.Exists(n => n.Status != TaskStatus.RanToCompletion && n.Status != TaskStatus.Faulted)) {
                    Task faulted = tasks.FirstOrDefault(n => n.IsFaulted);
                    if(faulted != null) {
                        throw faulted.Exception?.InnerExceptions?[0];
                    }

                    if(errorHandling == ErrorHandlingEnum.Fail) {
                        var failed = requests.FirstOrDefault(n => n.Response?.Success == false);
                        if(failed != null) {
                            throw new Exception(failed.Response?.ErrorMessage);
                        }
                    }

                    Thread.Sleep(2000);
                }

                foreach(Task task in tasks) {
                    if(task.Status == TaskStatus.Faulted) {
                        throw task.Exception?.InnerExceptions?[0];
                    }
                }
            } catch(Exception ex) {
                throw new Exception(ex.Message + "/" + ex.StackTrace);
            }
        }

        private void Merge(string name) {
            Thread.CurrentThread.Name = name;
            CrmCommands commands = Connection.CrmCommandsFactory(connectionString);
            int first = -1;
            while(first < noOfRows) {
                lock (lockObject) {
                    first = nextPointer;
                    nextPointer += batchSize;
                }

                if(first < noOfRows) {
                    List<OrganizationRequestModel> localRows = requests.Skip(first).Take(batchSize).ToList();
                    int last = localRows.Last().Index;
                    try {
                        commands.Merge(mergeEntity, localRows, batchSize, true, true);
                    } catch(Exception ex) {
                        throw ex;
                    } finally {
                        lock (lockObject) {
                            currentLast = last > currentLast ? last : currentLast;
                        }
                    }
                }
            }
        }

        private bool UpsertIntersect(string name) {
            Thread.CurrentThread.Name = name;
            CrmCommands commands = Connection.CrmCommandsFactory(connectionString);
            int first = -1;
            while (first < noOfRows) {
                lock (lockObject) {
                    first = nextPointer;
                    nextPointer += batchSize;
                }
                if (first < noOfRows) {
                    List<OrganizationRequestModel> localRows = requests.Skip(first).Take(batchSize).ToList();
                    int last = localRows.Last().Index;

                    List<OrganizationRequestModel> forExecute = new List<OrganizationRequestModel>();
                    if(operationType == OperationTypeEnum.Upsert) {
                        commands.FindRecordIdByMatching(localRows.Select(n => (AttributeMatchingRequestModel)n).ToList(), batchSize);
                        forExecute = localRows.Where(n => n.FoundedIds.Count == 0).ToList();
                    }
                    else if(operationType == OperationTypeEnum.Create || operationType == OperationTypeEnum.Delete) {
                        forExecute = localRows;
                    }

                    OperationTypeEnum opType = operationType == OperationTypeEnum.Delete ? OperationTypeEnum.Disassociate : OperationTypeEnum.Associate;

                    try {
                        if(forExecute.Count > 0) {
                            commands.OrganizationReq(opType, ComplexMappingEnum.PrimaryKey, forExecute, batchSize, true, true, ignoreNullValuedFields, this.relationship);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        lock (lockObject)
                        {
                            currentLast = last > currentLast ? last : currentLast;
                        }
                    }
                }
            }
            return true;
        }

        private void Update(object name) {
            Thread.CurrentThread.Name = name.ToString();
            CrmCommands commands = Connection.CrmCommandsFactory(connectionString);
            int first = -1;
            while(first < noOfRows) {
                lock (lockObject) {
                    first = nextPointer;
                    nextPointer += batchSize;
                }
                
                try {
                    if(first < noOfRows) {
                        List<OrganizationRequestModel> localRows = requests.Skip(first).Take(batchSize).ToList();

                        List<OrganizationRequestModel> forUpdateStatus = new List<OrganizationRequestModel>();
                        if (operationType == OperationTypeEnum.Create || operationType == OperationTypeEnum.Upsert) {
                            var stateCodeRows = localRows.Where(n => n.Row.Columns.Exists(k => k.LogicalName.ToLower() == "statecode")).ToList();
                            foreach(OrganizationRequestModel row in stateCodeRows) {
                                int stateCode = -1;
                                CrmColumn state = row.Row.Columns.FirstOrDefault(n => n.LogicalName.ToLower() == "statecode");
                                CrmColumn status = row.Row.Columns.FirstOrDefault(n => n.LogicalName.ToLower() == "statuscode");

                                if(state != null) {
                                    if(state.Value.GetType() != typeof(int)) {
                                        state.Value = int.TryParse(state.Value.ToString(), out stateCode);
                                    } else {
                                        stateCode = (int)state.Value;
                                    }
                                }

                                if(stateCode > 0) {
                                    OrganizationRequestModel newRequest = new OrganizationRequestModel {
                                        AlternateKeys = row.AlternateKeys,
                                        Entity = row.Entity,
                                        Index = row.Index,
                                        PrimaryId = row.PrimaryId,
                                        Row = new CrmRow {
                                            Columns = new List<CrmColumn>(new CrmColumn[] {
                                                state
                                            })
                                        }
                                    };
                                    row.Row.Columns.Remove(state);
                                    if (status != null) {
                                        row.Row.Columns.Remove(status);
                                        newRequest.Row.Columns.Add(status);
                                    }

                                    forUpdateStatus.Add(newRequest);
                                }
                            }
                        }
                        try {
                            commands.OrganizationReq(operationType, idMapping, localRows, batchSize, true, true, ignoreNullValuedFields);
                            if(forUpdateStatus.Count > 0) {
                                commands.OrganizationReq(OperationTypeEnum.Update, idMapping, forUpdateStatus, batchSize, true, true, ignoreNullValuedFields);
                            }
                        } catch(Exception ex) {
                            throw new Exception(ex.Message + ex.StackTrace);
                        }
                        finally {
                            lock (lockObject) {
                                int last = localRows.Last().Index;
                                currentLast = last > currentLast ? last : currentLast;
                            }
                        }
                    }
                } catch(Exception ex) {
                    byte[] additionalData = null;
                    metadata.PostLogMessage(MyLogEntryName, metadata.Name, ex.Message, DateTime.Now, DateTime.Now, 0, ref additionalData);
                    throw ex;
                } 
            }
        }
    }
}
