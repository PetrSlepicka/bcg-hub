using System.ComponentModel.DataAnnotations;

namespace BcgHub.Api.Application;

public sealed record ManagedUserDto(Guid Id, string FullName, string Email, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, bool IsCurrentUser);
public sealed record CreatedManagedUserDto(ManagedUserDto User, string TemporaryPassword);
public sealed record CreateManagedUserRequest([Required, StringLength(200)] string FullName, [Required, EmailAddress, StringLength(320)] string Email, [StringLength(200, MinimumLength = 12)] string? Password);
public sealed record UpdateManagedUserRequest([Required, StringLength(200)] string FullName, [Required, EmailAddress, StringLength(320)] string Email, bool IsActive, [StringLength(200, MinimumLength = 12)] string? Password);
