using HiveHub.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HiveHub.Infrastructure.BackgroundJobs;

public class RoomCleanupService : BackgroundService
{
    private readonly ISpyGameRepository _repository;
    private readonly ILogger<RoomCleanupService> _logger;

    public RoomCleanupService(ISpyGameRepository repository, ILogger<RoomCleanupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkInterval = TimeSpan.FromMinutes(1);
        var expirationThreshold = TimeSpan.FromMinutes(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removedCount = await _repository.RemoveInactiveRoomsAsync(expirationThreshold);

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup: Removed {Count} inactive rooms.", removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during room cleanup.");
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }
}
