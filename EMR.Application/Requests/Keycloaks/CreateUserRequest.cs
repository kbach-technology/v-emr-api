using EMR.Domain.Enums;
using EMR.Domain.Shared;

namespace EMR.Application.Requests.Keycloaks;

public record CreateUserRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    DateTime DateOfBirth,
    string Gender,
    string ProImg);

public record KeyCloakCreateUserRequest(
    string PhoneNumber,
    string Email,
    string Password,
    string RoleName);