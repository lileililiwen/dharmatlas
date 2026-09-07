namespace Dharmatlas.Domain.Entities;

/// <summary>
/// The kind of historical object a contribution proposes to add or change. Kept to
/// the supported contribution types from the change proposal; anything else is
/// rejected at validation time so the system never accepts unsupported edits.
/// </summary>
public enum SubmissionType
{
    Source,
    Date,
    Name,
    Event,
    Relationship,
    Translation,
    Institution
}
