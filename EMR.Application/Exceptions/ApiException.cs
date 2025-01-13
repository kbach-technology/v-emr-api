using System.Globalization;

namespace EMR.Application.Exceptions;

public class ApiException : Exception
{
    public ApiException()
    {
    }

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, params object[] args)
        : base(string.Format(CultureInfo.CurrentCulture, message, args))
    {
    }
}