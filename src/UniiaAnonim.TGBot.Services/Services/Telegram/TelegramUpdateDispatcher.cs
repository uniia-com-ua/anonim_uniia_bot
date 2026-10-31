using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Helpers;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Shared.Configuration;
using UniiaAnonim.TGBot.Shared.Consts;

namespace UniiaAnonim.TGBot.Application.Services.Telegram;

/// <summary>
/// Implements the <see cref="ITelegramUpdateDispatcher"/> to route incoming Telegram updates
/// to the appropriate handling strategy. Automatically manages the thread's culture
/// based on the user's preferred language code before processing the update.
/// </summary>
public sealed class TelegramUpdateDispatcher(
    IOptions<GeneralOptions> options,
    IEnumerable<ITelegramUpdateStrategy> strategies,
    IDefaultTelegramUpdateStrategy defaultTelegramUpdateStrategy,
    IBannedUserService bannedUserService)
    : ITelegramUpdateDispatcher
{
    private readonly GeneralOptions _options = options.Value;
    private readonly ITelegramUpdateStrategy[] _strategies = [.. strategies];

    /// <inheritdoc/>
    public async Task DispatchAsync(Update update, CancellationToken ct = default)
    {
        var userId = update.Message?.From?.Id ??
                     update.CallbackQuery?.From?.Id ??
                     update.EditedMessage?.From?.Id ??
                     update.MyChatMember?.From?.Id;

        if (userId.HasValue && await bannedUserService.IsBannedAsync(userId.Value, ct))
        {
            return;
        }

        var languageCode = ResolveLanguageCode(update);

        using var cultureScope = new CultureScope(languageCode);

        foreach (var strategy in _strategies)
        {
            if (await strategy.CanHandleAsync(update, ct))
            {
                await strategy.HandleAsync(update, ct);
                return;
            }
        }

        await defaultTelegramUpdateStrategy.HandleAsync(update, ct);
    }

    private string ResolveLanguageCode(Update update)
    {
        var userLanguage = update.Message?.From?.LanguageCode ??
                           update.CallbackQuery?.From?.LanguageCode ??
                           update.EditedMessage?.From?.LanguageCode ??
                           update.MyChatMember?.From?.LanguageCode;

        return userLanguage switch
        {
            { } lang when lang.StartsWith("uk", StringComparison.OrdinalIgnoreCase) => "uk",
            { } lang when lang.StartsWith("en", StringComparison.OrdinalIgnoreCase) => "en",
            _ => _options.DefaultLanguage ?? ApplicationConsts.DefaultLanguage,
        };
    }
}