using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Infrastructure.Configurations;

/// <summary>
/// Configures the entity mapping for the <see cref="BannedUser"/> entity.
/// </summary>
public class BannedUserConfiguration : IEntityTypeConfiguration<BannedUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BannedUser> builder)
    {
        builder.ToTable("BannedUsers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TelegramIdHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.HasIndex(x => x.TelegramIdHash)
               .IsUnique();

        builder.Property(x => x.Reason)
               .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
               .IsRequired();
    }
}
