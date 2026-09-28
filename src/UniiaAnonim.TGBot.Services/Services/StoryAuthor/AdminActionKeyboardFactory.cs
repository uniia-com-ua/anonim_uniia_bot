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
    private readonly IStringLocalizer<Messages> _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    /// <inheritdoc/>
    public InlineKeyboardMarkup CreateModerationKeyboard(Guid storyId)
    {
        return storyId == Guid.Empty
            ? throw new ArgumentException("Story ID cannot be empty.", nameof(storyId))
            : new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData(_localizer["str0028"], ButtonPrefixes.GetAdminPublishStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(_localizer["str0029"], ButtonPrefixes.GetAdminEditStoryButtonPrefix(storyId))],
            [InlineKeyboardButton.WithCallbackData(_localizer["str0030"], ButtonPrefixes.GetAdminRejectStoryButtonPrefix(storyId))]
        ]);
    }
}