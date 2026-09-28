using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Infrastructure.Persistence;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

/// <summary>
/// Provides repository implementation for managing <see cref="AuthorAgreement"/> entities.
/// </summary>
/// <param name="appDbContext">The application database context.</param>
public class AuthorAgreementRepository(
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
}