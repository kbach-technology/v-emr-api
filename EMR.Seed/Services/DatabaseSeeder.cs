using System;
using System.Threading;
using EMR.Application.Interfaces.Repositories;
using EMR.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace EMR.Seed.Services;

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IUnitOfWork<string> _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IUnitOfWork<string> unitOfWork,
        ICurrentUserService currentUser,
        ILogger<DatabaseSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize(CancellationToken cancellationToken)
    {
    }
}