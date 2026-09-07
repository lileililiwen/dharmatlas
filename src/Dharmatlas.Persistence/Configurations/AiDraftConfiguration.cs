using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Stores AI drafts as their own table with the immutable suggestion, input
/// provenance, model metadata, and the human decision trail. The status column is
/// the single source of truth for whether a draft has been promoted to review; the
/// drafts never write to published tables themselves.
/// </summary>
public class AiDraftConfiguration : IEntityTypeConfiguration<AiDraft>
{
    public void Configure(EntityTypeBuilder<AiDraft> builder)
    {
        builder.ToTable("ai_drafts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(d => d.Kind).HasColumnName("kind").HasConversion<string>();
        builder.Property(d => d.InputReferenceId).HasColumnName("input_reference_id").HasConversion(new EntityIdConverter());
        builder.Property(d => d.InputText).HasColumnName("input_text").IsRequired();
        builder.Property(d => d.Model).HasColumnName("model").IsRequired();
        builder.Property(d => d.ModelVersion).HasColumnName("model_version").IsRequired();
        builder.Property(d => d.PromptRef).HasColumnName("prompt_ref");
        builder.Property(d => d.SuggestionJson).HasColumnName("suggestion_json").IsRequired();
        builder.Property(d => d.Confidence).HasColumnName("confidence");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>();
        builder.OwnsMany(d => d.Decisions, nav =>
        {
            nav.ToTable("ai_draft_decisions");
            nav.Property(x => x.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
            nav.Property(x => x.ReviewerId).HasColumnName("reviewer_id").HasConversion(new EntityIdConverter());
            nav.Property(x => x.Decision).HasColumnName("decision").HasConversion<string>();
            nav.Property(x => x.Reason).HasColumnName("reason").IsRequired();
            nav.Property(x => x.Timestamp).HasColumnName("timestamp");
            nav.Property(x => x.EditedSuggestionJson).HasColumnName("edited_suggestion_json");
            nav.WithOwner().HasForeignKey("ai_draft_id");
        });

        builder.HasIndex(d => d.Status, "ix_ai_drafts_status");
        builder.HasIndex(d => d.InputReferenceId, "ix_ai_drafts_input");
    }
}
