using Microsoft.AspNetCore.Http;

namespace UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

public record StoryAuthorDto(
    long TelegramId,
    string Story,
    List<IFormFile>? Files);
