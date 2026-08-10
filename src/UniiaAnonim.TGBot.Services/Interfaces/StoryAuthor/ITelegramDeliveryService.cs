using Microsoft.AspNetCore.Http;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

namespace UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;

/// <summary>
/// Defines a service responsible for delivering text and optional media attachments to a Telegram chat,
/// handling Telegram API limits and stream lifecycles.
/// </summary>
public interface ITelegramDeliveryService
{
    /// <summary>
    /// Asynchronously delivers a story text and optional media files to the admin channel.
    /// </summary>
    /// <param name="chatId">The identifier of the target admin chat.</param>
    /// <param name="text">The text content of the story.</param>
    /// <param name="files">An optional list of media files to attach.</param>
    /// <param name="keyboard">The inline keyboard for moderation actions.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="TelegramMessageIds"/> with the sent message identifiers.</returns>
    Task<TelegramMessageIds> DeliverToAdminAsync(long chatId, string text, IReadOnlyList<IFormFile>? files, InlineKeyboardMarkup keyboard, CancellationToken ct);
}