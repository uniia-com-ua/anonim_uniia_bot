namespace UniiaAnonim.TGBot.Shared.Dtos.Telegram;

/// <summary>
/// Represents a definition for a bot command.
/// </summary>
/// <param name="Command">The command string (e.g., "/start").</param>
/// <param name="DescriptionKey">The localization key for the command description.</param>
public record BotCommandDefinition(string Command, string DescriptionKey);