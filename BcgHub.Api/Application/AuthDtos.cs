using System.ComponentModel.DataAnnotations;

namespace BcgHub.Api.Application;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(320)] public string Email { get; init; } = "";
    [Required, StringLength(200, MinimumLength = 8)] public string Password { get; init; } = "";
}

public sealed record CurrentUserDto(Guid Id, string Email, string FullName);
public sealed record AuthenticatedUser(Guid Id, string Email, string FullName);
