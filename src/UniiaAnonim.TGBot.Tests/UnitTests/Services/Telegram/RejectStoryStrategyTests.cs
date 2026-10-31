using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for <see cref="RejectStoryStrategy"/>.
/// </summary>
public class RejectStoryStrategyTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IStoryAuthorService _storyAuthorServiceSubstitute;
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly ILogger<RejectStoryStrategy> _loggerSubstitute;
    private readonly RejectStoryStrategy _strategy;

    public RejectStoryStrategyTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _storyAuthorServiceSubstitute = Substitute.For<IStoryAuthorService>();
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _loggerSubstitute = Substitute.For<ILogger<RejectStoryStrategy>>();

        _localizerSubstitute["str0007"].Returns(new LocalizedString("str0007", "Invalid format"));
        _localizerSubstitute["str0008"].Returns(new LocalizedString("str0008", "Story rejected notify"));
        _localizerSubstitute["str0009", Arg.Any<object[]>()].Returns(callInfo => new LocalizedString("str0009", $"Updated: {callInfo.Arg<object[]>()[0]}"));
        _localizerSubstitute["str0010"].Returns(new LocalizedString("str0010", "Story rejected toast"));
        _localizerSubstitute["str0011"].Returns(new LocalizedString("str0011", "Error rejecting story"));

        _strategy = new RejectStoryStrategy(
            _botClientSubstitute,
            _storyAuthorServiceSubstitute,
            _localizerSubstitute,
            _loggerSubstitute);
    }

    [Fact]
    public async Task CanHandleAsyncWhenRejectPrefixReturnsTrue()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = ButtonPrefixes.GetAdminRejectStoryButtonPrefix(storyId),
            },
        };

        // Act
        var result = await _strategy.CanHandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanHandleAsyncWhenDifferentPrefixReturnsFalse()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = "other_callback_prefix",
            },
        };

        // Act
        var result = await _strategy.CanHandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsyncWhenValidDeletesStoryAndNotifiesAuthor()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        const long authorTelegramId = 998877;
        const long adminChatId = -100999;
        const int adminMessageId = 42;

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_reject_1",
                Data = ButtonPrefixes.GetAdminRejectStoryButtonPrefix(storyId),
                Message = new Message
                {
                    Id = adminMessageId,
                    Chat = new Chat { Id = adminChatId },
                    Text = "Initial submission text",
                },
            },
        };

        _storyAuthorServiceSubstitute.GetDecryptedTelegramIdAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(authorTelegramId);

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        // 1. Deletes story from repository via service
        await _storyAuthorServiceSubstitute.Received(1).DeleteAsync(storyId, Arg.Any<CancellationToken>());

        // 2. Sends notification to author
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == authorTelegramId && req.Text == _localizerSubstitute["str0008"].Value),
            Arg.Any<CancellationToken>());

        // 3. Edits admin message
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<EditMessageTextRequest>(req => req.ChatId.Identifier == adminChatId && req.MessageId == adminMessageId),
            Arg.Any<CancellationToken>());

        // 4. Answers callback query
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_reject_1" && req.Text == _localizerSubstitute["str0010"].Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsyncWhenInvalidStoryIdAnswersCallbackQueryWithError()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_bad_guid",
                Data = $"{ButtonPrefixes.AdminRejectStoryPrefix}not-a-valid-guid",
                Message = new Message
                {
                    Id = 1,
                    Chat = new Chat { Id = 123 },
                },
            },
        };

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        await _storyAuthorServiceSubstitute.DidNotReceiveWithAnyArgs().DeleteAsync(default, TestContext.Current.CancellationToken);
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_bad_guid" && req.Text == _localizerSubstitute["str0007"].Value),
            Arg.Any<CancellationToken>());
    }
}
