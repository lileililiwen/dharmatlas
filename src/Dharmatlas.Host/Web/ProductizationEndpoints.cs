using System.Net;
using System.Text;
using Dharmatlas.Domain;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Host.Web;

/// <summary>
/// Published-web productization endpoints: crawler sitemap over published
/// records only, and index.html fallback with per-entity meta injection so
/// shared deep links carry title/description/canonical without SSR.
/// Drafts and rejected contributions live in Submissions and never enter
/// the sitemap or meta lookup, which reads Entities only.
/// </summary>
public static class ProductizationEndpoints
{
    private static readonly IReadOnlySet<string> EntityPlurals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "persons", "places", "texts", "institutions", "traditions", "events",
    };

    public static void MapProductization(this WebApplication app)
    {
        app.MapGet("/sitemap.xml", async (DharmatlasDbContext db, HttpContext http) =>
        {
            var rows = await db.Entities.AsNoTracking()
                .OrderBy(e => e.Id.Value)
                .Take(5000)
                .Select(e => new { e.Id, e.Type })
                .ToListAsync(http.RequestAborted);
            var origin = $"{http.Request.Scheme}://{http.Request.Host}";
            var xml = SitemapBuilder.BuildXml(rows.Select(r => $"/{r.Type.ToString().ToLowerInvariant()}s/{r.Id}"), origin);
            return Results.Content(xml, "application/xml");
        });
    }

    public static bool TryParseEntityPath(string path, out string plural, out string id)
    {
        plural = string.Empty;
        id = string.Empty;
        var trimmed = (path ?? string.Empty).Split('?', '#')[0].Trim();
        var segments = trimmed.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2) return false;
        if (!EntityPlurals.Contains(segments[0])) return false;
        if (segments[1].Length == 0 || segments[1].Length > 200) return false;
        plural = segments[0].ToLowerInvariant();
        id = segments[1];
        return true;
    }

    public static async Task<IResult> ServeIndexWithMetaAsync(HttpContext http, string webRoot)
    {
        var indexPath = Path.Combine(webRoot, "index.html");
        if (!File.Exists(indexPath)) return Results.NotFound();
        var html = await File.ReadAllTextAsync(indexPath, http.RequestAborted);
        if (TryParseEntityPath(http.Request.Path.Value ?? string.Empty, out _, out var rawId)
            && EntityId.TryParse(Uri.UnescapeDataString(rawId), out var entityId))
        {
            try
            {
                using var scope = http.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DharmatlasDbContext>();
                var row = await db.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entityId, http.RequestAborted);
                if (row is not null)
                {
                    var names = await db.EntityNames.AsNoTracking()
                        .Where(n => n.EntityId == entityId)
                        .ToListAsync(http.RequestAborted);
                    var canonical = Domain.EntityNameReadModel.CanonicalName(names) ?? entityId.ToString();
                    var origin = $"{http.Request.Scheme}://{http.Request.Host}";
                    html = MetaInjector.Inject(html,
                        $"{canonical} — {row.Type} | Dharmatlas",
                        "A source-first record in the open historical atlas of Buddhism.",
                        $"{origin}{http.Request.Path}");
                }
            }
            catch
            {
                // Database unavailable: serve the static shell without leaking anything.
            }
        }
        return Results.Content(html, "text/html");
    }
}

/// <summary>Pure sitemap builder over caller-supplied published routes.</summary>
public static class SitemapBuilder
{
    public static string BuildXml(IEnumerable<string> routes, string origin)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        foreach (var route in routes)
        {
            var loc = $"{origin.TrimEnd('/')}{route}";
            sb.AppendLine($"<url><loc>{WebUtility.HtmlEncode(loc)}</loc></url>");
        }
        sb.AppendLine("</urlset>");
        return sb.ToString();
    }
}

/// <summary>Pure head-tag injector for the static shell.</summary>
public static class MetaInjector
{
    public static string Inject(string html, string title, string description, string canonical)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeDescription = WebUtility.HtmlEncode(description);
        var safeCanonical = WebUtility.HtmlEncode(canonical);
        html = ReplaceOrInsert(html, "<title>", "</title>", $"<title>{safeTitle}</title>");
        html = UpsertMetaName(html, "description", safeDescription);
        html = UpsertMetaProperty(html, "og:title", safeTitle);
        html = UpsertMetaProperty(html, "og:description", safeDescription);
        html = UpsertMetaProperty(html, "og:url", safeCanonical);
        if (html.Contains("rel=\"canonical\"", StringComparison.Ordinal))
        {
            return html;
        }
        return html.Replace("</head>", "<link rel=\"canonical\" href=\"" + safeCanonical + "\" /></head>", StringComparison.Ordinal);
    }

    private static string ReplaceOrInsert(string html, string open, string close, string replacement)
    {
        var start = html.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf(close, StringComparison.OrdinalIgnoreCase);
        if (start >= 0 && end > start)
        {
            return html[..start] + replacement + html[(end + close.Length)..];
        }
        return html.Replace("</head>", replacement + "</head>", StringComparison.Ordinal);
    }

    private static string UpsertMetaName(string html, string name, string content)
    {
        var marker = "name=\"" + name + "\"";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return html.Replace("</head>", "<meta name=\"" + name + "\" content=\"" + content + "\" /></head>", StringComparison.Ordinal);
        }
        var tagStart = html.LastIndexOf('<', index);
        var tagEnd = html.IndexOf('>', index);
        if (tagStart < 0 || tagEnd < 0) return html;
        return html[..tagStart] + "<meta name=\"" + name + "\" content=\"" + content + "\" />" + html[(tagEnd + 1)..];
    }

    private static string UpsertMetaProperty(string html, string property, string content)
    {
        var marker = "property=\"" + property + "\"";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return html.Replace("</head>", "<meta property=\"" + property + "\" content=\"" + content + "\" /></head>", StringComparison.Ordinal);
        }
        var tagStart = html.LastIndexOf('<', index);
        var tagEnd = html.IndexOf('>', index);
        if (tagStart < 0 || tagEnd < 0) return html;
        return html[..tagStart] + "<meta property=\"" + property + "\" content=\"" + content + "\" />" + html[(tagEnd + 1)..];
    }
}
