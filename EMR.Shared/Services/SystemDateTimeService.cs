using System;
using EMR.Shared.Interfaces;

namespace EMR.Shared.Services;

public class SystemDateTimeService : IDateTimeService
{
    public DateTime NowUtc => DateTime.UtcNow;
}