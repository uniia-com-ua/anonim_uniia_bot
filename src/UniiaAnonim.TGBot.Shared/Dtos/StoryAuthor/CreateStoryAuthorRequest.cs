using Microsoft.AspNetCore.Http;

namespace UniiaAnonim.TGBot.Shared.Dtos.StoryAuthor;

public record CreateStoryAuthorRequest(
    string Story,
    List<IFormFile>? Files);