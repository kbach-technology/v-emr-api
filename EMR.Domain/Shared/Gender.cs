using System;

namespace EMR.Domain.Shared;

public sealed record Gender
{
    public string Value { get; }

    private Gender() { }
    private Gender(string value)
    {
        Value = value;
    }

    public static Gender Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Gender cannot be empty");

        // Normalize the input to uppercase for consistent comparison
        var normalizedValue = value.Trim().ToUpperInvariant();
        
        // Validate against allowed values
        if (!IsValidGender(normalizedValue))
            throw new ArgumentException($"Invalid gender value: {value}. Must be one of: MALE, FEMALE, OTHER");

        return new Gender(normalizedValue);
    }

    private static bool IsValidGender(string value) =>
        value is "MALE" or "FEMALE" or "OTHER";

    // Optional: Provide static instances for common values
    public static Gender Male => new Gender("MALE");
    public static Gender Female => new Gender("FEMALE");
    public static Gender Other => new Gender("OTHER");

    // Optional: Helper method to check if it's a specific gender
    public bool IsMale() => Value == "MALE";
    public bool IsFemale() => Value == "FEMALE";
    public bool IsOther() => Value == "OTHER";
}