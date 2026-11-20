using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Framely.Media.Internal;

internal sealed class MediaStorageHealthCheck(IPhotoStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.IsWritable()
            ? HealthCheckResult.Healthy("Media store is writable.")
            : HealthCheckResult.Unhealthy("Media store is not writable."));
}
