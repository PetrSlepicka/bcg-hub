namespace BcgHub.Api.Domain;

public enum EmailDirection { Inbound, Outbound }

public sealed class EmailAccountSettings : Entity
{
    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public string ImapServer { get; set; } = "";
    public int ImapPort { get; set; } = 993;
    public bool ImapUseSsl { get; set; } = true;
    public string ImapUsername { get; set; } = "";
    public string ProtectedImapPassword { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class EmailMessage : Entity
{
    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public Guid? BusinessPartnerId { get; set; }
    public BusinessPartner? BusinessPartner { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public EmailDirection Direction { get; set; }
    public string ExternalId { get; set; } = "";
    public uint ImapUid { get; set; }
    public string Mailbox { get; set; } = "INBOX";
    public string FromAddress { get; set; } = "";
    public string? FromName { get; set; }
    public string ToAddress { get; set; } = "";
    public string? CcAddress { get; set; }
    public string Subject { get; set; } = "";
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }
}
