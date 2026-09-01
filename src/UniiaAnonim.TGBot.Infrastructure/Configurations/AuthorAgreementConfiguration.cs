using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Infrastructure.Configurations;

/// <summary>
/// Configures the entity mapping for the <see cref="AuthorAgreement"/> entity.
/// </summary>
public class AuthorAgreementConfiguration : IEntityTypeConfiguration<AuthorAgreement>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AuthorAgreement> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuthorIdHash)
               .IsRequired();

        builder.HasIndex(x => x.AuthorIdHash)
               .IsUnique();

        builder.Property(x => x.HasAcceptedRules)
               .IsRequired();
    }
}