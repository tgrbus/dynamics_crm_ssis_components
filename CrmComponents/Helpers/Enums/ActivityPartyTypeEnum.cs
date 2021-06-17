using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Helpers.Enums
{
    public enum ActivityPartyTypeEnum
    {
        from = 1, //Sender
        to = 2,//ToRecipeint
        cc = 3,//CcRecipent
        bcc = 4,//BccRecipient
        requiredattendees = 5,//RequiredAttendee
        optionalattendees = 6,//OptionalAttendee
        organizer = 7,//Organizer
        regarding = 8,//
        owner = 9,
        resources = 10,//resource
        customers = 11,//customer
        partners = 12, //partners
    }
}
