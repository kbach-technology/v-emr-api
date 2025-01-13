using EMR.Application.Requests;
using EMR.Application.Responses;
using EMR.Domain.Entities.Settings;

namespace EMR.Application.Mappings;

public class AppVersionProfile : Profile
{
    public AppVersionProfile()
    {
        CreateMap<AppVersionResponse, AppVersion>().ReverseMap();
        CreateMap<AppVersionRequest, AppVersion>().ReverseMap();
    }
}