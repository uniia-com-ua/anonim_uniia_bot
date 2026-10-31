using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for <see cref="UnbanUserStrategy"/>.
/// </summary>
public class UnbanUserStrategyTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IStoryAuthorService _storyAuthorServiceSubstitute;
    private readonly IBannedUserService _bannedUserServiceSubstitute;
    private readonly IAdminActionKeyboardFactory _keyboardFactorySubstitute;
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly ILogger<UnbanUserStrategy> _loggerSubstitute;
    private readonly UnbanUserStrategy _strategy;

    public UnbanUserStrategyTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _storyAuthorServiceSubstitute = Substitute.For<IStoryAuthorService>();
        _bannedUserServiceSubstitute = Substitute.For<IBannedUserService>();
        _keyboardFactorySubstitute = Substitute.For<IAdminActionKeyboardFactory>();
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _loggerSubstitute = Substitute.For<ILogger<UnbanUserStrategy>>();

        _localizerSubstitute["str0007"].Returns(new LocalizedString("str0007", "Invalid format"));
        _localizerSubstitute["str0042"].Returns(new LocalizedString("str0042", "User unbanned"));
        _localizerSubstitute["str0043"].Returns(new LocalizedString("str0043", "Error occurred"));

        _keyboardFactorySubstitute.CreateModerationKeyboard(Arg.Any<Guid>())
            .Returns(new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData("Publish", "publish")],
                [InlineKeyboardButton.WithCallbackData("Reject", "reject")],
            ]));

        _strategy = new UnbanUserStrategy(
            _botClientSubstitute,
            _storyAuthorServiceSubstitute,
            _bannedUserServiceSubstitute,
            _keyboardFactorySubstitute,
            _localizerSubstitute,
            _loggerSubstitute);
    }

    /// <summary>
    /// Verifies that CanHandleAsync returns true when callback data starts with the admin unban prefix.
    /// </summary>
    [Fact]
    public async Task CanHandleAsyncWhenAdminUnbanPrefixReturnsTrue()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = ButtonPrefixes.GetAdminUnbanUserButtonPrefix(storyId),
            },
        };

        // Act
        var result = await _strategy.CanHandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that CanHandleAsync returns false when callback data has a different prefix.
    /// </summary>
    [Fact]
    public async Task CanHandleAsyncWhenDifferentPrefixReturnsFalse()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = "other_prefix_456",
            },
        };

        // Act
        var result = await _strategy.CanHandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that HandleAsync unbans the author, restores the standard moderation keyboard,
    /// cleans up the status suffix from the message text, and answers the callback query.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWhenValidUnbansAuthorRestoresModerationKeyboardAndUpdatesMessage()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        const long authorTelegramId = 999888777;
        const long adminChatId = -100987654321;
        const int adminMessageId = 505;

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_unban_1",
                Data = ButtonPrefixes.GetAdminUnbanUserButtonPrefix(storyId),
                Message = new Message
                {
                    Id = adminMessageId,
                    Chat = new Chat { Id = adminChatId },
                    Text = "Sample Story Content\n\n━━━━━━━━━━━━━━━\n<b>[Статус: ⛔ Заблоковано]</b>",
                },
            },
        };

        _storyAuthorServiceSubstitute.GetDecryptedTelegramIdAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(authorTelegramId);

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        // 1. Author must be unbanned
        await _bannedUserServiceSubstitute.Received(1).UnbanUserAsync(authorTelegramId, Arg.Any<CancellationToken>());

        // 2. Standard moderation keyboard must be created
        _keyboardFactorySubstitute.Received(1).CreateModerationKeyboard(storyId);

        // 3. Callback query answered
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_unban_1" && req.Text == _localizerSubstitute["str0042"].Value),
            Arg.Any<CancellationToken>());

        // 4. Admin message updated with original text and restored moderation keyboard
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<EditMessageTextRequest>(req => req.ChatId.Identifier == adminChatId && req.MessageId == adminMessageId && req.ReplyMarkup != null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that HandleAsync answers with an error when story ID format in callback data is invalid.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWhenInvalidStoryIdAnswersCallbackQueryWithError()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_unban_invalid",
                Data = $"{ButtonPrefixes.AdminUnbanUserPrefix}invalid-guid",
                Message = new Message
                {
                    Id = 606,
                    Chat = new Chat { Id = -100555666 },
                    Text = "Story",
                },
            },
        };

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        await _bannedUserServiceSubstitute.DidNotReceiveWithAnyArgs().UnbanUserAsync(default, TestContext.Current.CancellationToken);
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_unban_invalid" && req.Text == _localizerSubstitute["str0007"].Value),
            Arg.Any<CancellationToken>());
    }
}
