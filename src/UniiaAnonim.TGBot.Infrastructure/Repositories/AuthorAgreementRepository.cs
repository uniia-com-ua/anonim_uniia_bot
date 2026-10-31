using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Infrastructure.Persistence;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

/// <summary>
/// Provides repository implementation for managing <see cref="AuthorAgreement"/> entities.
/// </summary>
/// <param name="appDbContext">The application database context.</param>
public sealed class AuthorAgreementRepository(
    AppDbContext appDbContext)
    : GenericRepository<AuthorAgreement>(appDbContext),
    IAuthorAgreementRepository
{
    /// <summary>
    /// Asynchronously checks whether the author with the specified identifier hash has accepted the rules.
    /// </summary>
    /// <param name="authorIdHash">The unique hash of the author to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the author has accepted the rules; otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> HasAcceptedRulesAsync(string authorIdHash, CancellationToken ct = default)
        => await DbSet
                .AsNoTracking()
                .AnyAsync(x => x.AuthorIdHash == authorIdHash && x.HasAcceptedRules, ct);

    /// <inheritdoc/>
    public async Task AcceptRulesAsync(string authorIdHash, CancellationToken ct = default)
    {
        var existing = await DbSet.FirstOrDefaultAsync(x => x.AuthorIdHash == authorIdHash, ct);
        if (existing is null)
        {
            var newAgreement = new AuthorAgreement
            {
                AuthorIdHash = authorIdHash,
                HasAcceptedRules = true,
            };

            DbSet.Add(newAgreement);
            await Context.SaveChangesAsync(ct);
        }
        else if (!existing.HasAcceptedRules)
        {
            existing.HasAcceptedRules = true;
            await Context.SaveChangesAsync(ct);
        }
    }
}