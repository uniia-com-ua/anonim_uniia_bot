using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Interfaces.Telegram;
using UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for <see cref="BanUserStrategy"/>.
/// </summary>
public class BanUserStrategyTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IStoryAuthorService _storyAuthorServiceSubstitute;
    private readonly IBannedUserService _bannedUserServiceSubstitute;
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly ILogger<BanUserStrategy> _loggerSubstitute;
    private readonly BanUserStrategy _strategy;

    public BanUserStrategyTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _storyAuthorServiceSubstitute = Substitute.For<IStoryAuthorService>();
        _bannedUserServiceSubstitute = Substitute.For<IBannedUserService>();
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _loggerSubstitute = Substitute.For<ILogger<BanUserStrategy>>();

        _localizerSubstitute["str0007"].Returns(new LocalizedString("str0007", "Invalid format"));
        _localizerSubstitute["str0038"].Returns(new LocalizedString("str0038", "User banned"));
        _localizerSubstitute["str0039", Arg.Any<object[]>()].Returns(callInfo => new LocalizedString("str0039", $"Formatted: {callInfo.Arg<object[]>()[0]}"));
        _localizerSubstitute["str0040"].Returns(new LocalizedString("str0040", "Error occurred"));

        _strategy = new BanUserStrategy(
            _botClientSubstitute,
            _storyAuthorServiceSubstitute,
            _bannedUserServiceSubstitute,
            _localizerSubstitute,
            _loggerSubstitute);
    }

    /// <summary>
    /// Verifies that CanHandleAsync returns true when callback data starts with the admin ban prefix.
    /// </summary>
    [Fact]
    public async Task CanHandleAsyncWhenAdminBanPrefixReturnsTrue()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = ButtonPrefixes.GetAdminBanUserButtonPrefix(storyId),
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
                Data = "other_prefix_123",
            },
        };

        // Act
        var result = await _strategy.CanHandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that HandleAsync successfully bans the user, keeps the story in the database,
    /// attaches the unban button to the moderation message, answers the callback query,
    /// and critically sends NO message to the author.
    /// </summary>
    [Fact]
    public async Task HandleAsyncWhenValidBansAuthorAttachesUnbanButtonAndSendsNoNotificationToAuthor()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        const long authorTelegramId = 444555666;
        const long adminChatId = -100123456789;
        const int adminMessageId = 101;

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_123",
                Data = ButtonPrefixes.GetAdminBanUserButtonPrefix(storyId),
                Message = new Message
                {
                    Id = adminMessageId,
                    Chat = new Chat { Id = adminChatId },
                    Text = "Sample Story Text",
                },
            },
        };

        _storyAuthorServiceSubstitute.GetDecryptedTelegramIdAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(authorTelegramId);

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        // 1. Author must be banned in the system
        await _bannedUserServiceSubstitute.Received(1).BanUserAsync(authorTelegramId, Arg.Any<string?>(), Arg.Any<CancellationToken>());

        // 2. Story must NOT be deleted so it can be unbanned later
        await _storyAuthorServiceSubstitute.DidNotReceiveWithAnyArgs().DeleteAsync(default, Arg.Any<CancellationToken>());

        // 3. Callback query answered for the admin
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_123" && req.Text == _localizerSubstitute["str0038"].Value),
            Arg.Any<CancellationToken>());

        // 4. Admin message updated with status and unban button
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<EditMessageTextRequest>(req => req.ChatId.Identifier == adminChatId && req.MessageId == adminMessageId && req.ReplyMarkup != null),
            Arg.Any<CancellationToken>());

        // 5. CRITICAL: NO message must be sent to the author (Requirement 2.1)
        await _botClientSubstitute.DidNotReceiveWithAnyArgs().SendRequest(
            Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == authorTelegramId),
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
                Id = "cb_invalid",
                Data = $"{ButtonPrefixes.AdminBanUserPrefix}not-a-guid",
                Message = new Message
                {
                    Id = 202,
                    Chat = new Chat { Id = -100111222 },
                    Text = "Story",
                },
            },
        };

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        await _bannedUserServiceSubstitute.DidNotReceiveWithAnyArgs().BanUserAsync(default, default, TestContext.Current.CancellationToken);
        await _storyAuthorServiceSubstitute.DidNotReceiveWithAnyArgs().DeleteAsync(default, TestContext.Current.CancellationToken);
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_invalid" && req.Text == _localizerSubstitute["str0007"].Value),
            Arg.Any<CancellationToken>());
    }
}
