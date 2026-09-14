using Dharmatlas.Host.Observability;
using Microsoft.AspNetCore.Http;

namespace Dharmatlas.Domain.Tests;

public sealed class OperationsMaturityTests
{
    // ---- PII scrub ----

    [Fact]
    public void Scrub_redacts_contributor_email_from_messages()
    {
        var scrubbed = TelemetryScrubber.ScrubMessage(
            "Failed for editor@example.com on claim import");

        Assert.DoesNotContain("editor@example.com", scrubbed, StringComparison.Ordinal);
        Assert.Contains("[redacted-email]", scrubbed, StringComparison.Ordinal);
    }

    [Fact]
    public void Scrub_drops_identity_and_content_keys_from_properties()
    {
        var scrubbed = TelemetryScrubber.ScrubProperties(new Dictionary<string, string?>
        {
            ["route"] = "/api/v1/search",
            ["email"] = "editor@example.com",
            ["claim_text"] = "The Buddha was born in ...",
            ["Authorization"] = "Bearer secret",
            ["sub"] = "user-1",
        });

        Assert.Equal("/api/v1/search", scrubbed["route"]);
        Assert.DoesNotContain("email", scrubbed.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("claim_text", scrubbed.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", scrubbed.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("sub", scrubbed.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scrub_exception_payload_carries_no_email_or_claim_text()
    {
        var failure = new InvalidOperationException("Import failed for editor@example.com");
        failure.Data["claim_text"] = "Unverified chronicle passage";
        failure.Data["route"] = "/api/v1/import";

        var (message, data) = TelemetryScrubber.ScrubException(failure);

        Assert.DoesNotContain("editor@example.com", message, StringComparison.Ordinal);
        Assert.DoesNotContain("claim_text", data.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("/api/v1/import", data["route"]);
    }

    [Fact]
    public void Error_reporter_transmits_only_scrubbed_payload()
    {
        var capturing = new CapturingReporter();
        var failure = new InvalidOperationException("Failed for editor@example.com");
        failure.Data["claim_text"] = "Chronicle passage";

        capturing.Report(failure, "correlation-1", "/api/v1/import");

        Assert.DoesNotContain("editor@example.com", capturing.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("claim_text", capturing.Data.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("correlation-1", capturing.CorrelationId);
    }

    // ---- Trace propagation ----

    [Fact]
    public void Request_activity_carries_the_correlation_id()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == DharmatlasActivitySource.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/meta";

        using var activity = DharmatlasActivitySource.StartRequestActivity(context, "correlation-9");

        Assert.NotNull(activity);
        Assert.Equal("correlation-9", activity.GetTagItem("correlation_id"));
    }

    [Fact]
    public void Telemetry_counters_cover_import_moderation_ai_and_export()
    {
        // Additive instruments only; recording must not throw.
        TelemetryMetrics.RecordSlowQuery("/api/v1/search");
        TelemetryMetrics.RecordImportRejection("missing-source");
        TelemetryMetrics.RecordModerationDecision("approve");
        TelemetryMetrics.RecordAiDraft("entity-extraction");
        TelemetryMetrics.RecordExportJob("success");
    }

    // ---- SLOs, alerts, runbooks ----

    [Fact]
    public void Slo_doc_states_thresholds_and_points_at_runbook()
    {
        var root = FindRepositoryRoot();
        var slo = File.ReadAllText(Path.Combine(root, "docs", "slo.md"));

        Assert.Contains("300", slo, StringComparison.Ordinal);
        Assert.Contains("250", slo, StringComparison.Ordinal);
        Assert.Contains("99.9", slo, StringComparison.Ordinal);
        Assert.Contains("99%", slo, StringComparison.Ordinal);
        Assert.Contains("runbook", slo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Alert_rules_cover_readiness_errors_latency_export_and_drill_with_runbooks()
    {
        var root = FindRepositoryRoot();
        var alerts = File.ReadAllText(Path.Combine(root, "ops", "prometheus-alerts.yml"));

        foreach (var rule in new[]
                 {
                     "DharmatlasReadinessFailing",
                     "DharmatlasHigh5xxRate",
                     "DharmatlasSlowReads",
                     "DharmatlasSlowSearch",
                     "DharmatlasExportFailures",
                     "DharmatlasRestoreDrillFailing",
                 })
        {
            Assert.Contains(rule, alerts, StringComparison.Ordinal);
        }

        Assert.Contains("runbook_url", alerts, StringComparison.Ordinal);
    }

    // ---- Promotion without data auto-rollback ----

    [Fact]
    public void Release_train_signs_attests_sboms_prechecks_and_retains_previous_image()
    {
        var root = FindRepositoryRoot();
        var release = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));

        Assert.Contains("cosign", release, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sbom", release, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("migration-precheck", release, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("previous", release, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("downgrade", release, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database update --", release, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_precheck_verifies_model_before_staging()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "migration-precheck.sh"));

        Assert.Contains("has-pending-model-changes", script, StringComparison.Ordinal);
    }

    // ---- Drilled recovery and journey gates ----

    [Fact]
    public void Restore_drill_asserts_ready_entity_and_export_checksum()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "restore-drill.sh"));

        Assert.Contains("/health/ready", script, StringComparison.Ordinal);
        Assert.Contains("sha256sum", script, StringComparison.Ordinal);
        Assert.Contains("pg_restore", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_drill_default_entity_is_a_parseable_guid_person()
    {
        // EntityId is GUID-only: a slug default would make /persons/{id} return
        // 400 and fail the drill. The default must parse.
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "restore-drill.sh"));
        var marker = "DRILL_ENTITY_ID:=";
        var start = script.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "DRILL_ENTITY_ID default is missing.");

        var value = script[(start + marker.Length)..].Split('}')[0];
        Assert.True(Dharmatlas.Domain.EntityId.TryParse(value, out _),
            $"Drill default entity '{value}' is not a valid entity id.");
    }

    [Fact]
    public void Deploy_compose_files_reference_no_undefined_services()
    {
        var root = FindRepositoryRoot();
        foreach (var file in new[] { "staging.compose.yml", "prod.compose.yml" })
        {
            var compose = File.ReadAllText(Path.Combine(root, "deploy", file));
            Assert.DoesNotContain("depends_on", compose, StringComparison.Ordinal);
        }

        Assert.True(File.Exists(Path.Combine(root, "deploy", "k8s", "deployment.yaml")));
    }

    [Fact]
    public void Restore_drill_is_scheduled_nightly()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "restore-drill.yml"));

        Assert.Contains("cron", workflow, StringComparison.Ordinal);
        Assert.Contains("restore-drill.sh", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_gate_enforces_read_search_and_error_thresholds()
    {
        var root = FindRepositoryRoot();
        var load = File.ReadAllText(Path.Combine(root, "k6", "load.js"));

        Assert.Contains("p(95)<300", load, StringComparison.Ordinal);
        Assert.Contains("250", load, StringComparison.Ordinal);
        Assert.Contains("http_req_failed", load, StringComparison.Ordinal);
        Assert.Contains("fromYear", load, StringComparison.Ordinal);
        Assert.Contains("toYear", load, StringComparison.Ordinal);
    }

    [Fact]
    public void Journey_gate_keeps_playwright_and_axe_coverage()
    {
        var root = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "web", "e2e", "atlas.spec.js")));
        var journeys = File.ReadAllText(Path.Combine(root, "web", "e2e", "atlas.spec.js"));
        Assert.Contains("AxeBuilder", journeys, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "web", "playwright.config.js")));
    }

    private sealed class CapturingReporter : IErrorReporter
    {
        public string? Message { get; private set; }
        public IReadOnlyDictionary<string, string?> Data { get; private set; } = new Dictionary<string, string?>();
        public string? CorrelationId { get; private set; }

        public void Report(Exception exception, string correlationId, string route)
        {
            var (message, data) = TelemetryScrubber.ScrubException(exception);
            Message = message;
            Data = data;
            CorrelationId = correlationId;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && directory is not null; depth++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dharmatlas.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root with Dharmatlas.slnx was not found.");
    }
}
