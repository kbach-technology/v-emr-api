using System.Collections.Generic;
using EMR.Application.Responses.Audit;

namespace EMR.Application.Interfaces.Services;

public interface IAuditService
{
    Task<IResult<IEnumerable<AuditResponse>>> GetCurrentUserTrailsAsync(string userId, string searchString);
    Task<IResult<IEnumerable<AuditResponse>>> GetAllTrailsAsync(string searchString);

    Task<IResult<string>> ExportToExcelAsync(string userId, string searchString = "", bool searchInOldValues = false,
        bool searchInNewValues = false);
}