using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

public interface IAuthorAgreementRepository
    : IGenericRepository<AuthorAgreement>
{
    /// <summary>
    /// Asynchronously checks whether the author with the specified identifier hash has accepted the rules.
    /// </summary>
    /// <param name="authorIdHash">The unique hash of the author to check.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the author has accepted the rules; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> HasAcceptedRulesAsync(string authorIdHash, CancellationToken ct = default);
}