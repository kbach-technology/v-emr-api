using System;

namespace EMR.Domain.Shared;

public sealed record DateOfBirth
{
    public DateTime Value { get; }
    public int Age => CalculateAge();

    private DateOfBirth() { }
    private DateOfBirth(DateTime value)
    {
        // Ensure the stored value is UTC
        Value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static DateOfBirth Create(DateTime value)
    {
        // Convert input to UTC for comparison if it isn't already
        var utcNow = DateTime.UtcNow.Date;
        var normalizedValue = DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

        if (normalizedValue > utcNow)
            throw new ArgumentException("Date of birth cannot be in the future");
        
        if (normalizedValue.Year < 1900)
            throw new ArgumentException("Date of birth must be after 1900");

        return new DateOfBirth(normalizedValue);
    }

    private int CalculateAge()
    {
        // Use UTC for consistency
        var today = DateTime.UtcNow.Date;
        var birthDate = Value.Date;
        
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        
        return age;
    }
}