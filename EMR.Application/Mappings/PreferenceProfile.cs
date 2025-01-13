using EMR.Application.Requests;
using EMR.Application.Resources;
using EMR.Domain.Entities.Settings;

namespace EMR.Application.Mappings;

public class PreferenceProfile : Profile
{
    public PreferenceProfile()
    {
        CreateMap<PreferenceRequest, Preference>();
        CreateMap<Preference, PerferenceResponse>();
    }
}