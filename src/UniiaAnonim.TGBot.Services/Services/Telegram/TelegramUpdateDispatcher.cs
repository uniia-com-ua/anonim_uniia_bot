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
    IDefaultTelegramUpdateStrategy defaultTelegramUpdateStrategy)
    : ITelegramUpdateDispatcher
{
    private readonly GeneralOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task DispatchAsync(Update update, CancellationToken ct = default)
    {
        var languageCode = _options.DefaultLanguage ?? ApplicationConsts.DefaultLanguage;

        using (new CultureScope(languageCode))
        {
            foreach (var strategy in strategies)
            {
                if (await strategy.CanHandleAsync(update, ct))
                {
                    await strategy.HandleAsync(update, ct);
                    return;
                }
            }

            await defaultTelegramUpdateStrategy.HandleAsync(update, ct);
        }
    }
}