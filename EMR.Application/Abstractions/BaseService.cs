using EMR.Application.Interfaces.Repositories;
using EMR.Shared.Interfaces;
using Serilog;

namespace EMR.Application.Abstractions;

public abstract class BaseService<T> : IDisposable
{
    protected readonly ICurrentUserService _currentUserService;
    protected readonly IDateTimeService _dateTimeService;
    protected readonly IStringLocalizer<T> _localizer;
    protected readonly IMapper _mapper;
    protected readonly ILogger _trace;
    protected readonly IUnitOfWork<string> _unitOfWork;
    private bool _disposed;

    public BaseService(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUserService,
        IStringLocalizer<T> localizer,
        IDateTimeService dateTimeService,
        IMapper mapper,
        ILogger trace)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _dateTimeService = dateTimeService;
        _mapper = mapper;
        _trace = trace;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _unitOfWork?.Dispose();
        }

        _disposed = true;
    }

    // Finalizer as a backup for resource cleanup
    ~BaseService()
    {
        Dispose(false);
    }
}