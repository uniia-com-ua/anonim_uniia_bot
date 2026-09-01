namespace UniiaAnonim.TGBot.Domain.Models;

/// <summary>
/// Represents a story author entity in the domain model.
/// </summary>
public class AuthorAgreement
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the hash of the author identifier.
    /// </summary>
    public string AuthorIdHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has accepted the rules.
    /// </summary>
    public bool HasAcceptedRules { get; set; }
}