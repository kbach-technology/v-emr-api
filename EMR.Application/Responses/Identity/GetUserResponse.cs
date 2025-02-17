using EMR.Application.Responses.Based;
using EMR.Domain.Enums;
using EMR.Domain.Shared;

namespace EMR.Application.Responses.Identity;

public class GetUserResponse : BaseResponse
{
    public string Id { get; set; }
    public string UserNo { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateOfBirth DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string ProImg { get; set; }
    public bool IsActive { get; set; }
}