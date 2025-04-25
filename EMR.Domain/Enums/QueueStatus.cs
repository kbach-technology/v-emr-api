namespace EMR.Domain.Enums;

public enum QueueStatus
{
    Waiting = 1,
    Called = 2,
    InProgress = 3,
    Completed = 4,
    NoShow = 5,
    Cancelled = 6,
    Rescheduled = 7,
    OnHold = 8,
    Transferred = 9,
}