using EMR.Application.Responses.Audit;
using EMR.Domain.Entities.Audit;

namespace EMR.Application.Mappings;

public class AuditProfile : Profile
{
    public AuditProfile()
    {
        CreateMap<AuditResponse, Audit>().ReverseMap();
    }
}