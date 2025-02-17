using EMR.Application.Requests;
using EMR.Domain.Entities.Settings;

namespace EMR.Application.Mappings;

public class OtpRequestProfile : Profile
{
    public OtpRequestProfile()
    {
        CreateMap<OtpRequest, OTP>().ReverseMap();
        CreateMap<ResendOtpRequest, OTP>().ReverseMap();
        CreateMap<EmailOtpRequest, OTP>().ReverseMap();
        CreateMap<ResendEmailOtpRequest, OTP>().ReverseMap();
        CreateMap<ValidateOtpRequest, OTP>().ReverseMap();
        CreateMap<ValidateEmailOtpRequest, OTP>().ReverseMap();
    }
}