namespace UniiaAnonim.TGBot.Shared.Consts;

public static class ButtonPrefixes
{
    public const string UserApproveStoryPrefix = "user_approve_";

    public const string UserRejectStoryPrefix = "user_reject_";

    public const string AdminEditStoryPrefix = "admin_edit_";

    public const string AdminPublishStoryPrefix = "admin_publish_";

    public const string AdminRejectStoryPrefix = "admin_reject_";

    public static string GetUserApproveStoryButtonPrefix(Guid storyId) => $"{UserApproveStoryPrefix}{storyId}";

    public static string GetUserRejectStoryButtonPrefix(Guid storyId) => $"{UserRejectStoryPrefix}{storyId}";

    public static string GetAdminEditStoryButtonPrefix(Guid storyId) => $"{AdminEditStoryPrefix}{storyId}";

    public static string GetAdminPublishStoryButtonPrefix(Guid storyId) => $"{AdminPublishStoryPrefix}{storyId}";

    public static string GetAdminRejectStoryButtonPrefix(Guid storyId) => $"{AdminRejectStoryPrefix}{storyId}";
}
