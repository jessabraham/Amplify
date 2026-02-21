using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Domain.Entities.Identity;
using Amplify.Infrastructure.ExternalServices.AI;
using Amplify.Infrastructure.ExternalServices.MarketData;
using Amplify.Infrastructure.ExternalServices.Trading;
using Amplify.Infrastructure.Persistence;
using Amplify.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Amplify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Market data — Alpaca with sample data fallback
        services.Configure<AlpacaSettings>(config.GetSection(AlpacaSettings.SectionName));
        services.AddSingleton<AlpacaMarketDataService>();
        services.AddSingleton<SampleMarketDataService>();
        services.AddSingleton<CompositeMarketDataService>();
        services.AddSingleton<IMarketDataService>(sp => sp.GetRequiredService<CompositeMarketDataService>());

        // Services
        services.AddScoped<ITradeSignalService, TradeSignalService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IFeatureEngine, FeatureEngine>();
        services.AddScoped<IRegimeEngine, RegimeEngine>();
        services.AddScoped<IRegimeService, RegimeService>();
        services.AddScoped<IRiskEngine, RiskEngine>();

        services.AddScoped<IAIAdvisor, OllamaAIAdvisor>();// AI Advi

        services.AddScoped<IPatternDetector, PatternDetector>();

        services.AddHostedService<Amplify.Infrastructure.Services.BackgroundPatternScannerService>();

        services.AddHttpClient<IPatternAnalyzer, OllamaPatternAnalyzer>();

        // Simulation engine
        services.AddScoped<TradeSimulationService>();
        services.AddHostedService<SimulationResolverService>();

        return services;
    }
}