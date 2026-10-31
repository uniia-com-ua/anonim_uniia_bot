using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using UniiaAnonim.TGBot.Application.Interfaces.StoryAuthor;
using UniiaAnonim.TGBot.Application.Services.Telegram.EventStrategies;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Enums;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.Telegram;

/// <summary>
/// Unit tests for <see cref="AuthorReviewEditStrategy"/>.
/// </summary>
public class AuthorReviewEditStrategyTests
{
    private readonly ITelegramBotClient _botClientSubstitute;
    private readonly IStoryAuthorService _storyAuthorServiceSubstitute;
    private readonly IChannelRepository _channelRepositorySubstitute;
    private readonly IAdminActionKeyboardFactory _keyboardFactorySubstitute;
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly ILogger<AuthorReviewEditStrategy> _loggerSubstitute;
    private readonly AuthorReviewEditStrategy _strategy;

    public AuthorReviewEditStrategyTests()
    {
        _botClientSubstitute = Substitute.For<ITelegramBotClient>();
        _storyAuthorServiceSubstitute = Substitute.For<IStoryAuthorService>();
        _channelRepositorySubstitute = Substitute.For<IChannelRepository>();
        _keyboardFactorySubstitute = Substitute.For<IAdminActionKeyboardFactory>();
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _loggerSubstitute = Substitute.For<ILogger<AuthorReviewEditStrategy>>();

        _localizerSubstitute["str0023"].Returns(new LocalizedString("str0023", "Approved by author"));
        _localizerSubstitute["str0024"].Returns(new LocalizedString("str0024", "Rejected by author"));
        _localizerSubstitute["str0025"].Returns(new LocalizedString("str0025", "Toast approved"));
        _localizerSubstitute["str0026"].Returns(new LocalizedString("str0026", "Author rejection notify"));
        _localizerSubstitute["str0004", Arg.Any<object[]>()].Returns(callInfo => new LocalizedString("str0004", $"ModerationText: {callInfo.Arg<object[]>()[1]}"));

        _strategy = new AuthorReviewEditStrategy(
            _botClientSubstitute,
            _storyAuthorServiceSubstitute,
            _channelRepositorySubstitute,
            _keyboardFactorySubstitute,
            _localizerSubstitute,
            _loggerSubstitute);
    }

    [Fact]
    public async Task CanHandleAsyncWhenApproveOrRejectPrefixReturnsTrue()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        var approveUpdate = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = ButtonPrefixes.GetUserApproveStoryButtonPrefix(storyId),
            },
        };
        var rejectUpdate = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = ButtonPrefixes.GetUserRejectStoryButtonPrefix(storyId),
            },
        };

        // Act & Assert
        Assert.True(await _strategy.CanHandleAsync(approveUpdate, TestContext.Current.CancellationToken));
        Assert.True(await _strategy.CanHandleAsync(rejectUpdate, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsyncWhenApprovedUpdatesAdminMessageAndRemovesInlineKeyboard()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        const int adminMessageId = 77;
        const long adminChatId = -100555;
        const long authorChatId = 12345;

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_approve_1",
                Data = ButtonPrefixes.GetUserApproveStoryButtonPrefix(storyId),
                Message = new Message
                {
                    Id = 200,
                    Chat = new Chat { Id = authorChatId },
                    Text = $"{MessageMarkers.EditedHistoryTextMarker}Approved story body",
                },
            },
        };

        _channelRepositorySubstitute.GetByTypeAsync(ChannelType.AdminChannel, Arg.Any<CancellationToken>())
            .Returns(new Channel { ChannelId = adminChatId, Type = ChannelType.AdminChannel });

        _storyAuthorServiceSubstitute.GetAsync(storyId, Arg.Any<CancellationToken>())
            .Returns(new TelegramMessageIds(adminMessageId));

        _keyboardFactorySubstitute.CreateModerationKeyboard(storyId)
            .Returns(new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>()));

        // Act
        await _strategy.HandleAsync(update, TestContext.Current.CancellationToken);

        // Assert
        // 1. Removes keyboard from author message
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<EditMessageReplyMarkupRequest>(req => req.ChatId.Identifier == authorChatId && req.MessageId == 200),
            Arg.Any<CancellationToken>());

        // 2. Answers callback
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<AnswerCallbackQueryRequest>(req => req.CallbackQueryId == "cb_approve_1"),
            Arg.Any<CancellationToken>());

        // 3. Updates admin message
        await _botClientSubstitute.Received(1).SendRequest(
            Arg.Is<EditMessageTextRequest>(req => req.ChatId.Identifier == adminChatId && req.MessageId == adminMessageId),
            Arg.Any<CancellationToken>());
    }
}
