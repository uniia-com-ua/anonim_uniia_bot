using Microsoft.Extensions.Localization;
using NSubstitute;
using UniiaAnonim.TGBot.Application.Services.StoryAuthor;
using UniiaAnonim.TGBot.Shared.Consts;
using UniiaAnonim.TGBot.Shared.Resources;

namespace UniiaAnonim.TGBot.Tests.UnitTests.Services.StoryAuthor;

/// <summary>
/// Unit tests for <see cref="AdminActionKeyboardFactory"/>.
/// </summary>
public class AdminActionKeyboardFactoryTests
{
    private readonly IStringLocalizer<Messages> _localizerSubstitute;
    private readonly AdminActionKeyboardFactory _factory;

    public AdminActionKeyboardFactoryTests()
    {
        _localizerSubstitute = Substitute.For<IStringLocalizer<Messages>>();
        _localizerSubstitute["str0028"].Returns(new LocalizedString("str0028", "Publish"));
        _localizerSubstitute["str0029"].Returns(new LocalizedString("str0029", "Edit"));
        _localizerSubstitute["str0030"].Returns(new LocalizedString("str0030", "Reject"));
        _localizerSubstitute["str0037"].Returns(new LocalizedString("str0037", "Ban"));

        _factory = new AdminActionKeyboardFactory(_localizerSubstitute);
    }

    /// <summary>
    /// Verifies that CreateModerationKeyboard creates a keyboard with Publish, Edit, Reject, and Ban buttons.
    /// </summary>
    [Fact]
    public void CreateModerationKeyboardReturnsKeyboardWithAllFourActionButtonsIncludingBan()
    {
        // Arrange
        var storyId = Guid.NewGuid();

        // Act
        var keyboard = _factory.CreateModerationKeyboard(storyId);

        // Assert
        Assert.NotNull(keyboard);
        var buttonRows = keyboard.InlineKeyboard.ToList();
        Assert.Equal(4, buttonRows.Count);

        // Row 1: Publish
        Assert.Equal("Publish", buttonRows[0].First().Text);
        Assert.Equal(ButtonPrefixes.GetAdminPublishStoryButtonPrefix(storyId), buttonRows[0].First().CallbackData);

        // Row 2: Edit
        Assert.Equal("Edit", buttonRows[1].First().Text);
        Assert.Equal(ButtonPrefixes.GetAdminEditStoryButtonPrefix(storyId), buttonRows[1].First().CallbackData);

        // Row 3: Reject
        Assert.Equal("Reject", buttonRows[2].First().Text);
        Assert.Equal(ButtonPrefixes.GetAdminRejectStoryButtonPrefix(storyId), buttonRows[2].First().CallbackData);

        // Row 4: Ban (Requirement 1.1)
        Assert.Equal("Ban", buttonRows[3].First().Text);
        Assert.Equal(ButtonPrefixes.GetAdminBanUserButtonPrefix(storyId), buttonRows[3].First().CallbackData);
    }

    /// <summary>
    /// Verifies that CreateModerationKeyboard throws an ArgumentException if the story ID is empty.
    /// </summary>
    [Fact]
    public void CreateModerationKeyboardWhenStoryIdEmptyThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateModerationKeyboard(Guid.Empty));
    }
}
