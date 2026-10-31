namespace UniiaAnonim.TGBot.Shared.Consts;

/// <summary>
/// Defines centralized event identifiers for strongly-typed logging across the application.
/// </summary>
public static class LogEventIds
{
    // ==========================================
    // 1000s: Cache / Anti-DDoS / Storage
    // ==========================================

    /// <summary>
    /// User confirmed banned in database and caching status.
    /// </summary>
    public const int UserConfirmedBannedInDb = 1001;

    /// <summary>
    /// User has been banned and added to memory cache.
    /// </summary>
    public const int UserBannedAddedToCache = 1002;

    /// <summary>
    /// User has been unbanned and removed from memory cache.
    /// </summary>
    public const int UserUnbannedRemovedFromCache = 1003;

    // ==========================================
    // 2000s: Users & Bot Lifecycle / Channel Management
    // ==========================================

    /// <summary>
    /// An unsupported chat type was encountered for a chat.
    /// </summary>
    public const int UnsupportedChatTypeEncountered = 2001;

    /// <summary>
    /// Channel already exists in the database; skipping registration.
    /// </summary>
    public const int ChannelAlreadyExists = 2002;

    /// <summary>
    /// Channel does not exist in the database; skipping removal.
    /// </summary>
    public const int ChannelNotFoundForRemoval = 2003;

    // ==========================================
    // 3000s: Story Processing & Dispatching
    // ==========================================

    /// <summary>
    /// Failed to process a new story from a user.
    /// </summary>
    public const int CreateNewStoryFailed = 3001;

    /// <summary>
    /// No admin channels found when dispatching a story.
    /// </summary>
    public const int NoAdminChannelsFound = 3002;

    /// <summary>
    /// Story submission rejected because character length exceeds maximum allowed limit.
    /// </summary>
    public const int StoryLengthExceededLimit = 3003;

    /// <summary>
    /// Failed to dispatch story to admin channel.
    /// </summary>
    public const int DispatchStoryFailed = 3004;

    /// <summary>
    /// Public channel is not configured in the database.
    /// </summary>
    public const int PublicChannelNotConfigured = 3005;

    /// <summary>
    /// An error occurred while publishing a story.
    /// </summary>
    public const int PublishStoryFailed = 3006;

    /// <summary>
    /// An error occurred while rejecting a story.
    /// </summary>
    public const int RejectStoryFailed = 3007;

    /// <summary>
    /// Story author or interactive message ID not found.
    /// </summary>
    public const int StoryAuthorOrMessageNotFound = 3008;

    // ==========================================
    // 4000s: Admin Interaction & Replies & Moderation
    // ==========================================

    /// <summary>
    /// No story ID (Guid) found in replied message text.
    /// </summary>
    public const int StoryIdNotFoundInReplyMessage = 4001;

    /// <summary>
    /// Chat is not a registered admin channel.
    /// </summary>
    public const int AdminChannelNotFound = 4002;

    /// <summary>
    /// Failed to copy admin reply to user.
    /// </summary>
    public const int CopyAdminReplyFailed = 4003;

    /// <summary>
    /// No story ID (Guid) found in edit prompt.
    /// </summary>
    public const int StoryIdNotFoundInEditPrompt = 4004;

    /// <summary>
    /// Failed to send edited story to user.
    /// </summary>
    public const int SendEditedStoryFailed = 4005;

    /// <summary>
    /// Admin channel not found in database when updating message.
    /// </summary>
    public const int AdminChannelNotFoundForUpdateMessage = 4006;

    /// <summary>
    /// Failed to send user reply to admin channel.
    /// </summary>
    public const int SendUserReplyFailed = 4007;

    /// <summary>
    /// An error occurred while banning a user.
    /// </summary>
    public const int BanUserFailed = 4008;

    /// <summary>
    /// An error occurred while unbanning a user.
    /// </summary>
    public const int UnbanUserFailed = 4009;

    /// <summary>
    /// Admin channel not found in database when forwarding user reply.
    /// </summary>
    public const int AdminChannelNotFoundForUserReply = 4010;

    // ==========================================
    // 5000s: Telegram Updates, Webhooks & Commands
    // ==========================================

    /// <summary>
    /// Executing default Telegram update strategy.
    /// </summary>
    public const int ExecutingDefaultStrategy = 5001;

    /// <summary>
    /// Callback query is missing message context (with callback ID).
    /// </summary>
    public const int CallbackQueryMissingMessageContext = 5002;

    /// <summary>
    /// Callback query is missing context.
    /// </summary>
    public const int CallbackQueryMissingContext = 5003;

    /// <summary>
    /// Invalid story ID format in callback data.
    /// </summary>
    public const int InvalidStoryIdFormat = 5004;

    /// <summary>
    /// Update is missing message chat context.
    /// </summary>
    public const int UpdateMissingMessageChatContext = 5005;

    /// <summary>
    /// Setting up global Telegram bot commands.
    /// </summary>
    public const int SettingUpGlobalBotCommands = 5006;

    /// <summary>
    /// Successfully configured global bot commands with localization.
    /// </summary>
    public const int GlobalBotCommandsConfiguredSuccessfully = 5007;

    /// <summary>
    /// Failed to set global bot commands on startup.
    /// </summary>
    public const int FailedToSetGlobalBotCommands = 5008;

    /// <summary>
    /// Telegram webhook successfully set.
    /// </summary>
    public const int WebhookSuccessfullySet = 5009;

    /// <summary>
    /// Failed to set Telegram webhook.
    /// </summary>
    public const int FailedToSetWebhook = 5010;

    /// <summary>
    /// Telegram webhook successfully removed.
    /// </summary>
    public const int WebhookSuccessfullyRemoved = 5011;

    /// <summary>
    /// Failed to remove Telegram webhook on application shutdown.
    /// </summary>
    public const int FailedToRemoveWebhook = 5012;

    /// <summary>
    /// Callback query is missing message context (without callback ID).
    /// </summary>
    public const int CallbackQueryMessageContextMissing = 5013;

    // ==========================================
    // 6000s: Errors & Middleware
    // ==========================================

    /// <summary>
    /// A client error occurred while processing the request.
    /// </summary>
    public const int ClientErrorOccurred = 6001;

    /// <summary>
    /// An unhandled server exception occurred while processing the request.
    /// </summary>
    public const int UnhandledServerExceptionOccurred = 6002;

    // ==========================================
    // 8000s: Background Services
    // ==========================================

    /// <summary>
    /// An error occurred while processing a Telegram update.
    /// </summary>
    public const int UpdateProcessingFailed = 8001;
}
