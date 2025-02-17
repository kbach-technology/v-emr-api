using EMR.Application.Interfaces.Services;
using EMR.Application.Requests;
using EMR.Domain.Entities.Settings;

namespace EMR.API.Controllers.v1;

[ApiController]
[Route("api/app-version")]
public class AppVersionController : ControllerBase
{
    private readonly IAppVersionService _appVersionService;
    private readonly INumericService _numericService;
    private readonly IFinancialNumberService _financialNumberService;


    public AppVersionController(IAppVersionService appVersionService, INumericService numericService,
        IFinancialNumberService financialNumberService)
    {
        _appVersionService = appVersionService;
        _numericService = numericService;
        _financialNumberService = financialNumberService;
    }

    [HttpGet]
    //[Authorize(Policy = Permissions.AppVersion.View)]
    public async Task<IActionResult> Get(int pageNumber, int pageSize, string? searchString)
    {
        var receiptNumber = await _financialNumberService.GenerateReceiptNumberAsync<AppVersion>(
            r => r.VersionNumber, "RCP");
        var invoiceNumber = await _financialNumberService.GenerateInvoiceNumberAsync<AppVersion>(
            r => r.VersionNumber, "INV");
        var nunber = await _numericService.GenerateNumberAsync<AppVersion>(x => x.VersionNumber, "P", 7);
        var result = await _appVersionService.GetAllAsync(pageNumber, pageSize, searchString);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.AppVersion.View)]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _appVersionService.GetById(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.AppVersion.Create)]
    public async Task<IActionResult> Post(AppVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await _appVersionService.CreateAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.AppVersion.Amend)]
    public async Task<IActionResult> Put(string id, AppVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await _appVersionService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = Permissions.AppVersion.Amend)]
    public async Task<IActionResult> Patch(string id, CancellationToken cancellationToken)
    {
        var result = await _appVersionService.ToggleStatus(id, cancellationToken);
        return Ok(result);
    }
}