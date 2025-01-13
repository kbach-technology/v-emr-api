using EMR.Application.Requests;
using EMR.Domain.Entities.Settings;

namespace EMR.Application.Mappings;

public class OtpRequestProfile : Profile
{
    public OtpRequestProfile()
    {
        CreateMap<OtpRequest, OTP>().ReverseMap();
        CreateMap<ResendOtpRequest, OTP>().ReverseMap();
    }
}