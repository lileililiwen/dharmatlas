using System.Text.Json;
using System.Text.Json.Nodes;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Contributions.Engine;

/// <summary>
/// Pure review-workflow logic: decision transitions, conflict surfacing, and
/// immutable revision construction with field-level diffs. Independent of EF Core
/// so the gates and audit semantics can be unit tested without a database.
/// </summary>
public static class ReviewEngine
{
    /// <summary>
    /// Apply a reviewer decision to a pending/changed-requested submission. The
    /// returned submission reflects the new status and records the decision.
    /// </summary>
    public static Submission RecordDecision(Submission submission, ReviewDecision decision) =>
        submission switch
        {
            _ when decision.Decision == ReviewDecisionType.Approve => submission.WithDecision(decision),
            _ when decision.Decision == ReviewDecisionType.RequestChanges => submission.WithDecision(decision),
            _ when decision.Decision == ReviewDecisionType.Reject => submission.WithDecision(decision),
            _ => throw new DomainValidationException($"Unsupported decision {decision.Decision}.")
        };

    /// <summary>
    /// A new submission targeting an entity that already has an approved change
    /// with a different value conflicts and must be surfaced for review rather than
    /// silently merged.
    /// </summary>
    public static bool HasConflictWithApproved(
        IReadOnlyList<Submission> approvedForTarget, Submission proposed) =>
        approvedForTarget.Any(a => a.Id != proposed.Id &&
                                   !string.Equals(a.PayloadJson, proposed.PayloadJson, StringComparison.Ordinal));

    /// <summary>
    /// Build the immutable audit revision for an approved change. Captures the
    /// prior value, the field-level diff, the contributor and reviewer, the reason,
    /// and the supporting sources.
    /// </summary>
    public static Revision BuildRevision(
        EntityId targetId,
        string priorValueJson,
        EntityId contributorId,
        EntityId reviewerId,
        string reason,
        IReadOnlyList<EntityId> sourceIds,
        string? changedFieldsJson,
        DateTimeOffset timestamp) =>
        new(
            targetId,
            priorValueJson,
            contributorId,
            reason,
            timestamp,
            reviewerId: reviewerId,
            changedFieldsJson: changedFieldsJson,
            sourceIds: sourceIds);

    /// <summary>
    /// Compute the field-level diff between a prior value and the proposed payload.
    /// For a brand-new entity (no prior value) the diff is a single "created"
    /// marker; otherwise it lists the top-level JSON properties whose values differ.
    /// </summary>
    public static IReadOnlyList<string> DiffFields(string? priorValueJson, string proposedPayloadJson)
    {
        if (string.IsNullOrWhiteSpace(priorValueJson))
        {
            return new[] { "created" };
        }

        JsonNode? prior = SafeParse(priorValueJson);
        JsonNode? proposed = SafeParse(proposedPayloadJson);
        if (prior is null || proposed is null)
        {
            return new[] { "created" };
        }

        if (prior is not JsonObject priorObj || proposed is not JsonObject proposedObj)
        {
            return new[] { "value" };
        }

        var changed = new List<string>();
        foreach (var prop in proposedObj)
        {
            var before = priorObj[prop.Key];
            var after = prop.Value;
            if (!JsonEquals(before, after))
            {
                changed.Add(prop.Key);
            }
        }

        // A field removed from the proposed shape is also a change.
        foreach (var prop in priorObj)
        {
            if (!proposedObj.ContainsKey(prop.Key))
            {
                changed.Add(prop.Key);
            }
        }

        return changed.Distinct().ToList();
    }

    /// <summary>Serialize a field list to the JSON stored on the revision.</summary>
    public static string? SerializeChangedFields(IReadOnlyList<string> fields) =>
        fields.Count == 0 ? null : JsonSerializer.Serialize(fields.ToArray());

    private static bool JsonEquals(JsonNode? a, JsonNode? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.ToJsonString() == b.ToJsonString();
    }

    private static JsonNode? SafeParse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
