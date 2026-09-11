namespace Dharmatlas.Api.Models;

public enum ExportJobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

/// <summary>Observable state for a retry-safe export generation attempt.</summary>
public sealed record ExportJob
{
    public required string Id { get; init; }
    public ExportJobStatus Status { get; init; }
    public int Attempts { get; init; }
    public string? Error { get; init; }
    public string? Checksum { get; init; }
    public string? DownloadUrl { get; init; }

    public static ExportJob Create(string id) => new() { Id = id, Status = ExportJobStatus.Queued };

    public ExportJob Start() => Status is ExportJobStatus.Queued or ExportJobStatus.Failed
        ? this with { Status = ExportJobStatus.Running, Attempts = Attempts + 1, Error = null }
        : throw new InvalidOperationException("Only queued or failed export jobs can start.");

    public ExportJob Complete(string checksum, string downloadUrl) => Status == ExportJobStatus.Running
        ? this with { Status = ExportJobStatus.Completed, Checksum = checksum, DownloadUrl = downloadUrl }
        : throw new InvalidOperationException("Only running export jobs can complete.");

    public ExportJob Fail(string error) => Status == ExportJobStatus.Running
        ? this with { Status = ExportJobStatus.Failed, Error = error, Checksum = null, DownloadUrl = null }
        : throw new InvalidOperationException("Only running export jobs can fail.");
}
