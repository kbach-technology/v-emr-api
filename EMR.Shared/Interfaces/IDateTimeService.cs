using System;

namespace EMR.Shared.Interfaces;

public interface IDateTimeService
{
    DateTime NowUtc { get; }
}