using Amplify.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.Services;

/// <summary>
/// Background service that periodically resolves active simulated trades.
/// Runs every 5 minutes, checks all active trades against simulated prices.
/// When Alpaca is connected, this will check against real market data instead.
/// </summary>
public class SimulationResolverService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationResolverService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SimulationResolverService(IServiceScopeFactory scopeFactory, ILogger<SimulationResolverService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulationResolverService started. Resolving trades every {Interval} minutes.", _interval.TotalMinutes);

        // Wait a bit on startup to let everything initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var simulation = scope.ServiceProvider.GetRequiredService<TradeSimulationService>();

                var resolved = await simulation.ResolveActiveTradesAsync();
                if (resolved.Any())
                {
                    _logger.LogInformation("Resolved {Count} simulated trades: {Details}",
                        resolved.Count,
                        string.Join(", ", resolved.Select(t => $"{t.Asset} {t.Outcome} ({t.PnLPercent:F1}%)")));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving simulated trades");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}