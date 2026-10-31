using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Infrastructure.Configurations;

/// <summary>
/// Configures the entity mapping for the <see cref="StoryAuthor"/> entity.
/// </summary>
public class StoryAuthorConfiguration : IEntityTypeConfiguration<StoryAuthor>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StoryAuthor> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuthorId)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(x => x.AuthorIdHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.HasIndex(x => x.AuthorIdHash);
        builder.HasIndex(x => x.ChannelMessageId);
        builder.HasIndex(x => new { x.AuthorIdHash, x.Status });

        builder.HasMany(x => x.StoryMessages)
               .WithOne(x => x.StoryAuthor)
               .HasForeignKey(x => x.StoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}