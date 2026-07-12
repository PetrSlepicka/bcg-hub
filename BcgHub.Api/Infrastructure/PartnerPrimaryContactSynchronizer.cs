using BcgHub.Api.Domain;

namespace BcgHub.Api.Infrastructure;

internal static class PartnerPrimaryContactSynchronizer
{
    public static void Synchronize(BusinessPartner partner)
    {
        var primaryContact = partner.Contacts.FirstOrDefault(x => x.IsPrimary) ?? partner.Contacts.OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
        if (primaryContact is null && partner.Email is null && partner.Phone is null) return;

        primaryContact ??= new ContactPerson { BusinessPartnerId = partner.Id, FullName = partner.Name };
        foreach (var contact in partner.Contacts) contact.IsPrimary = contact == primaryContact;
        primaryContact.Email = partner.Email;
        primaryContact.Phone = partner.Phone;
        primaryContact.IsPrimary = true;
        if (!partner.Contacts.Contains(primaryContact)) partner.Contacts.Add(primaryContact);
    }
}
