namespace BcgHub.Api.Application;

public sealed class DomainValidationException(string message) : Exception(message);
public sealed class ConcurrencyConflictException(string message) : Exception(message);
