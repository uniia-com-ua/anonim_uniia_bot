using Microsoft.Extensions.Localization;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Application.Services.StoryAuthor;

/// <summary>
/// Implements the factory for creating story moderation inline keyboards with multi-language support.
/// </summary>
/// <param name="localizer">The string localizer for multi-language support.</param>
public sealed class AdminActionKeyboardFactory(IStringLocalizer<Messages> localizer)
    : IAdminActionKeyboardFactory
{
    /// <inheritdoc/>
    public InlineKeyboardMarkup CreateModerationKeyboard(Guid storyId)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return storyId == Guid.Empty
            ? throw new ArgumentException("Story ID cannot be empty.", nameof(storyId))
            : new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(localizer["str0028"], ButtonPrefixes.GetAdminPublishStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(localizer["str0029"], ButtonPrefixes.GetAdminEditStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(localizer["str0030"], ButtonPrefixes.GetAdminRejectStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(localizer["str0037"], ButtonPrefixes.GetAdminBanUserButtonPrefix(storyId))]
        ]);
    }
}