using System.Collections.Generic;
using EMR.Domain.Contracts;
using EMR.Domain.Enums;

namespace EMR.Domain.Entities.Notifications;

public class NotificationType : AuditableEntity<string>
{
    public NotificationTypeEnum NotificationTypeEnum { get; set; }

    public string Name { get; set; }

    public string Icon { get; set; }

    public List<Notification> Notifications { get; set; }
}