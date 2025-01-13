using Microsoft.Extensions.Localization;

namespace EMR.API.Localization;

internal class ServerLocalizer<T> where T : class
{
    public ServerLocalizer(IStringLocalizer<T> localizer)
    {
        Localizer = localizer;
    }

    public IStringLocalizer<T> Localizer { get; }
}