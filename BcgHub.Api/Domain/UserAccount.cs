namespace BcgHub.Api.Domain;

public sealed class UserAccount : Entity
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
