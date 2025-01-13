using EMR.Domain.Enums;

namespace EMR.Domain.Entities.Notifications;

public class Notification
{
    public string Title { get; set; }

    public string Category { get; set; }

    public string Text { get; set; }

    public bool IsRead { get; set; }

    public string UserId { get; set; }

    public string Deeplink { get; set; }

    public string Sub1 { get; set; }

    public bool IsImportant { get; set; }

    public NotificationTypeEnum Type { get; set; }

    public NotificationType NotificationType { get; set; }

    public string? HookId { get; set; }
}