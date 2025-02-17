using EMR.Application.Requests.Keycloaks;
using EMR.Domain.Enums;
using EMR.Domain.Shared;

namespace EMR.Application.Requests.Identity;

public record AmendUserRequest(
    string Id, 
    string FullName, 
    string Email, 
    string Phone, 
    DateTime DateOfBirth, 
    string Gender, 
    string ProImg, 
    bool IsActive);