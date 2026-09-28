using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Infrastructure.Configurations;

/// <summary>
/// Configures the entity mapping for the <see cref="Channel"/> entity.
/// </summary>
public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
               .IsRequired();

        builder.Property(x => x.ChannelId)
               .IsRequired();

        builder.HasIndex(x => x.ChannelId)
               .IsUnique();

        builder.HasIndex(x => x.Type);
    }
}