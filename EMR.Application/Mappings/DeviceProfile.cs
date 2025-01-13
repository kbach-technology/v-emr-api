using EMR.Application.Requests;
using EMR.Application.Responses;
using EMR.Domain.Entities.Settings;

namespace EMR.Application.Mappings;

public class DeviceProfile : Profile
{
    public DeviceProfile()
    {
        CreateMap<DeviceResponse, Device>().ReverseMap();
        CreateMap<DeviceRequest, Device>().ReverseMap();
    }
}