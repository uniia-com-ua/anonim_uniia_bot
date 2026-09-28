using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

public record StoryAuthorDto(
    long TelegramId,
    string Story,
    Dictionary<string, StoryMediaType>? Files);
