using System.Threading;

namespace EMR.Seed.Services;

public interface IDatabaseSeeder
{
    void Initialize(CancellationToken cancellationToken);
}