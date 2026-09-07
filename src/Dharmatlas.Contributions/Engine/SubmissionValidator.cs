using System.Text.Json;
using System.Text.Json.Nodes;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Contributions.Engine;

/// <summary>
/// Pure validation of a contribution submission before it enters the review queue.
/// Enforces supported types and the project-foundation rule that historical claims
/// cite at least one source. Kept free of EF Core so it can be unit tested.
/// </summary>
public static class SubmissionValidator
{
    public static ValidationResult Validate(Submission submission)
    {
        var errors = new List<string>();

        if (submission.SourceIds.Count == 0)
        {
            errors.Add("A submission must cite at least one source.");
        }

        if (string.IsNullOrWhiteSpace(submission.PayloadJson))
        {
            errors.Add("A submission requires a payload.");
        }
        else
        {
            JsonNode? node = null;
            try
            {
                node = JsonNode.Parse(submission.PayloadJson);
            }
            catch (JsonException)
            {
                errors.Add("The submission payload is not valid JSON.");
            }

            if (node is not null)
            {
                errors.AddRange(ValidateShape(submission.Type, node));
            }
        }

        return new ValidationResult { Errors = errors };
    }

    private static IEnumerable<string> ValidateShape(SubmissionType type, JsonNode node)
    {
        // Lightweight, shape-only checks. The service performs the concrete
        // domain mapping; here we only confirm the payload carries the fields the
        // type implies so obviously malformed contributions are rejected early.
        switch (type)
        {
            case SubmissionType.Date:
                if (node["DisplayExpression"]?.GetValue<string>() is null)
                {
                    yield return "A date correction requires a DisplayExpression.";
                }
                break;
            case SubmissionType.Source:
                if (node["Title"]?.GetValue<string>() is null)
                {
                    yield return "A source submission requires a Title.";
                }
                break;
            case SubmissionType.Event:
                if (node["Summary"]?.GetValue<string>() is null && node["DisplayExpression"]?.GetValue<string>() is null)
                {
                    yield return "An event submission requires a Summary or DisplayExpression.";
                }
                break;
            default:
                // Name, Relationship, Translation, Institution: non-empty JSON is enough here.
                break;
        }
    }
}

/// <summary>The outcome of validating a submission.</summary>
public sealed record ValidationResult
{
    public required IReadOnlyList<string> Errors { get; init; }
    public bool IsValid => Errors.Count == 0;
}
