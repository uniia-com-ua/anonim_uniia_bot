using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Infrastructure.Configurations;

/// <summary>
/// Configures the entity mapping for the <see cref="StoryFileEntity"/> entity.
/// </summary>
public class StoryFileEntityConfiguration : IEntityTypeConfiguration<StoryFileEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StoryFileEntity> builder)
    {
        builder.HasKey(x => new { x.StoryId, x.FileId });

        builder.Property(x => x.StoryId)
               .IsRequired();

        builder.Property(x => x.FileId)
               .IsRequired();

        builder.Property(x => x.Type)
               .IsRequired();

        builder.HasOne(x => x.StoryAuthor)
               .WithMany(x => x.StoryMessages)
               .HasForeignKey(x => x.StoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}