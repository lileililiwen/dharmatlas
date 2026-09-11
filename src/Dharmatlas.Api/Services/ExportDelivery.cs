using System.IO.Compression;
using System.Text.Json;
using Dharmatlas.Api.Models;

namespace Dharmatlas.Api.Services;

/// <summary>
/// Writes an already-created public snapshot directly to the response stream. The
/// serializer writes through gzip so the HTTP response never needs a second full
/// JSON string or an uncompressed byte buffer.
/// </summary>
public static class ExportDelivery
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static async Task WriteCompressedAsync(
        Stream destination,
        ExportSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        await using var gzip = new GZipStream(destination, CompressionLevel.Fastest, leaveOpen: true);
        await JsonSerializer.SerializeAsync(gzip, snapshot, Options, cancellationToken);
        await gzip.FlushAsync(cancellationToken);
    }
}
