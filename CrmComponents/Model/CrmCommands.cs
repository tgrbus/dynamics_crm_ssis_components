using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmComponents.Helpers.Enums;

namespace CrmComponents.Model
{
    public abstract class CrmCommands {
        public abstract List<Organization> RetrieveOrganizations(int timeoutSec = 30);
        public abstract bool WhoAmI(int timeoutSec = 30);
        public abstract List<Entity> RetrieveEntityList();
        public abstract List<CrmAttribute> RetrieveAttributesList(EntityModel model, Entity entity);
        public abstract FetchBatch RetrieveAllRecords(string entityName, string entitySetName, List<CrmAttribute> attributes, List<CrmAttribute> additionalAttributes = null, int pageNumber = 1, string cookie = null, int count = 1000, bool allColumns = false);
        public abstract FetchBatch RetrieveAllByFetch(string fetchXml, string entitySetName, List<CrmAttribute> attributes, int pageNumber = 1, int count = 5000, string cookie = null);
        public abstract void Merge(Entity entity, List<OrganizationRequestModel> requests, int batch = 5,
                                   bool continueOnError = true, bool returnResponses = true);
        public abstract void OrganizationReq(OperationTypeEnum operationType, ComplexMappingEnum idMapping, List<OrganizationRequestModel> requests, 
                                                 int batch = 5, bool continueOnError = true, bool returnResponses = true, bool ignoreNullValuedFields = false, 
                                                 Model.Relationship relationship = null);
        //todo: MULTIPLE MATCHES, NO MATCHES, INPUT IS NULL
        public abstract void FindRecordIdByMatching(List<AttributeMatchingRequestModel> requests, int batch = 10);
        public abstract bool CheckOrganizationUri();
    }
}
