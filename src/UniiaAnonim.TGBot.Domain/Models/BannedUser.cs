namespace UniiaAnonim.TGBot.Domain.Models;

/// <summary>
/// Represents a banned user entity in the domain model.
/// </summary>
public class BannedUser
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the hashed Telegram identifier of the banned user.
    /// </summary>
    public string TelegramIdHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional reason explaining why the user was banned.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user was banned.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
