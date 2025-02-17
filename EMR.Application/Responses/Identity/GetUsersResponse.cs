using EMR.Domain.Enums;
using EMR.Domain.Shared;

namespace EMR.Application.Responses.Identity;

public record GetUsersResponse( string Id, string UserNo, string FullName, string Email, string Phone, DateOfBirth DateOfBirth, Gender Gender, string ProImg, bool IsActive);