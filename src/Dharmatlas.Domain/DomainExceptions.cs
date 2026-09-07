namespace Dharmatlas.Domain;

/// <summary>
/// Raised when a value violates a domain invariant at the boundary (e.g. an
/// inverted date range, an unsupported certainty value, an unsourceable claim).
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}

/// <summary>
/// Raised when an object references an identifier that does not exist within the
/// accepted set (e.g. a relationship pointing at an unknown source).
/// </summary>
public class InvalidReferenceException : Exception
{
    public InvalidReferenceException(string message) : base(message) { }
}
