namespace CrmComponents.Helpers.Enums
{
    public enum OperationTypeEnum
    {
        Create,
        Update,
        Upsert,
        Delete,
        Associate,//only internal
        Disassociate,//only internal
        Merge
    }
}
