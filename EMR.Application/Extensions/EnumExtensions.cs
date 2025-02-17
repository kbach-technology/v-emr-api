using System.Collections.Generic;
using System.ComponentModel;

namespace EMR.Application.Extensions;

public static class EnumExtensions
{
    public static string ToDescriptionString(this Enum val)
    {
        var attributes = (DescriptionAttribute[])val.GetType().GetField(val.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attributes?.Length > 0
            ? attributes[0].Description
            : val.ToString();
    }

    public static string GetDisplayName(this Enum val)
    {
        var attributes = (DisplayAttribute[])val.GetType().GetField(val.ToString())
            ?.GetCustomAttributes(typeof(DisplayAttribute), false);

        return attributes?.Length > 0
            ? attributes[0].Name ?? val.ToString()
            : val.ToString();
    }

    public static T ToEnum<T>(this string value) where T : struct
    {
        if (Enum.TryParse<T>(value, true, out T result))
            return result;

        throw new ArgumentException($"Cannot convert {value} to enum {typeof(T)}");
    }

    public static bool TryParseEnum<T>(this string value, out T result) where T : struct
    {
        return Enum.TryParse(value, true, out result);
    }

    public static IEnumerable<T> GetValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    public static IDictionary<int, string> ToDictionary<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(t => Convert.ToInt32(t), t => t.ToString());
    }

    public static bool HasFlag<T>(this T value, T flag) where T : Enum
    {
        var valueAsInt = Convert.ToInt64(value);
        var flagAsInt = Convert.ToInt64(flag);
        return (valueAsInt & flagAsInt) == flagAsInt;
    }

    public static string GetDisplayDescription(this Enum val)
    {
        var attributes = (DisplayAttribute[])val.GetType().GetField(val.ToString())
            ?.GetCustomAttributes(typeof(DisplayAttribute), false);

        return attributes?.Length > 0
            ? attributes[0].Description ?? val.ToString()
            : val.ToString();
    }

    public static IDictionary<T, string> GetValuesWithDisplayNames<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(
                value => value,
                value => value.GetDisplayName());
    }
}