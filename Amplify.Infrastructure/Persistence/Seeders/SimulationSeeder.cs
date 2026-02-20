using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.Persistence.Seeders;

public static class SimulationSeeder
{
    public static async Task SeedDemoTradesAsync(IServiceProvider sp)
    {
        var context = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<Domain.Entities.Identity.ApplicationUser>>();
        var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Only seed if no simulated trades exist
        if (await context.SimulatedTrades.AnyAsync()) return;

        // Find the admin user (or first user)
        var admin = await userManager.GetUsersInRoleAsync("Admin");
        var user = admin.FirstOrDefault();
        if (user is null)
        {
            var allUsers = await userManager.Users.FirstOrDefaultAsync();
            if (allUsers is null) return;
            user = allUsers;
        }

        logger.LogInformation("Seeding demo simulated trades for user {UserId}...", user.Id);

        var trades = GenerateDemoTrades(user.Id);
        context.SimulatedTrades.AddRange(trades);
        await context.SaveChangesAsync();

        // Now build pattern performance aggregates from resolved trades
        await BuildPerformanceAggregatesAsync(context, user.Id);

        logger.LogInformation("Seeded {Count} demo simulated trades and performance stats.", trades.Count);
    }

    private static List<SimulatedTrade> GenerateDemoTrades(string userId)
    {
        var trades = new List<SimulatedTrade>();
        var random = new Random(42); // Deterministic for consistency

        var demoTrades = new[]
        {
            // ── WINNERS (aligned timeframes, good patterns) ──
            new DemoTrade("AAPL", SignalType.Long, 185.50m, 181.00m, 195.00m, TradeOutcome.HitTarget1, PatternType.BullishEngulfing, PatternDirection.Bullish, "Daily", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 52m, 8),
            new DemoTrade("NVDA", SignalType.Long, 720.00m, 695.00m, 780.00m, TradeOutcome.HitTarget1, PatternType.BullFlag, PatternDirection.Bullish, "Daily", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Breakout", 38m, 5),
            new DemoTrade("MSFT", SignalType.Long, 415.00m, 405.00m, 440.00m, TradeOutcome.HitTarget1, PatternType.MorningStar, PatternDirection.Bullish, "Weekly", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 45m, 12),
            new DemoTrade("TSLA", SignalType.Short, 265.00m, 278.00m, 240.00m, TradeOutcome.HitTarget1, PatternType.BearishEngulfing, PatternDirection.Bearish, "Daily", MarketRegime.VolExpansion, "All Bearish", "Aligned", "Bearish Stack", "Breakout", 72m, 4),
            new DemoTrade("GOOGL", SignalType.Long, 175.00m, 170.00m, 188.00m, TradeOutcome.HitTarget1, PatternType.DoubleBottom, PatternDirection.Bullish, "Daily", MarketRegime.MeanReversion, "All Bullish", "Aligned", "Mixed", "Normal", 35m, 15),
            new DemoTrade("AMZN", SignalType.Long, 195.00m, 189.00m, 210.00m, TradeOutcome.HitTarget1, PatternType.BullishEngulfing, PatternDirection.Bullish, "4H", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 48m, 6),
            new DemoTrade("META", SignalType.Long, 510.00m, 495.00m, 545.00m, TradeOutcome.HitTarget1, PatternType.CupAndHandle, PatternDirection.Bullish, "Weekly", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Breakout", 42m, 18),
            new DemoTrade("COIN", SignalType.Long, 220.00m, 205.00m, 255.00m, TradeOutcome.HitTarget1, PatternType.BullFlag, PatternDirection.Bullish, "Daily", MarketRegime.VolExpansion, "Mostly Bullish", "Mixed", "Bullish Stack", "Breakout", 55m, 7),
            new DemoTrade("AAPL", SignalType.Long, 192.00m, 187.00m, 205.00m, TradeOutcome.HitTarget1, PatternType.Hammer, PatternDirection.Bullish, "Daily", MarketRegime.MeanReversion, "Mostly Bullish", "Aligned", "Mixed", "Low", 40m, 9),
            new DemoTrade("NVDA", SignalType.Long, 750.00m, 730.00m, 800.00m, TradeOutcome.HitTarget1, PatternType.BullishEngulfing, PatternDirection.Bullish, "Weekly", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 62m, 11),

            // ── LOSERS (conflicting timeframes, bad setups) ──
            new DemoTrade("TSLA", SignalType.Long, 250.00m, 240.00m, 275.00m, TradeOutcome.HitStop, PatternType.BullishEngulfing, PatternDirection.Bullish, "4H", MarketRegime.Choppy, "Conflicting", "Conflicting", "Mixed", "Low", 55m, 3),
            new DemoTrade("GOOGL", SignalType.Short, 180.00m, 188.00m, 165.00m, TradeOutcome.HitStop, PatternType.BearishEngulfing, PatternDirection.Bearish, "4H", MarketRegime.Trending, "Conflicting", "Conflicting", "Bullish Stack", "Normal", 48m, 5),
            new DemoTrade("MSFT", SignalType.Long, 420.00m, 412.00m, 438.00m, TradeOutcome.HitStop, PatternType.Hammer, PatternDirection.Bullish, "4H", MarketRegime.Choppy, "Conflicting", "Conflicting", "Mixed", "Low", 35m, 2),
            new DemoTrade("AMZN", SignalType.Short, 200.00m, 208.00m, 185.00m, TradeOutcome.HitStop, PatternType.EveningStar, PatternDirection.Bearish, "Daily", MarketRegime.Trending, "Conflicting", "Conflicting", "Bullish Stack", "Normal", 60m, 4),
            new DemoTrade("AAPL", SignalType.Long, 188.00m, 183.00m, 198.00m, TradeOutcome.HitStop, PatternType.BullishEngulfing, PatternDirection.Bullish, "Daily", MarketRegime.Choppy, "Conflicting", "Mixed", "Mixed", "Low", 42m, 6),
            new DemoTrade("COIN", SignalType.Long, 235.00m, 218.00m, 270.00m, TradeOutcome.HitStop, PatternType.BullFlag, PatternDirection.Bullish, "4H", MarketRegime.VolExpansion, "Conflicting", "Conflicting", "Bearish Stack", "Normal", 50m, 2),

            // ── EXPIRED ──
            new DemoTrade("META", SignalType.Long, 520.00m, 505.00m, 555.00m, TradeOutcome.Expired, PatternType.AscendingTriangle, PatternDirection.Bullish, "Daily", MarketRegime.Choppy, "Mostly Bullish", "Mixed", "Mixed", "Low", 44m, 30),
            new DemoTrade("TSLA", SignalType.Short, 258.00m, 270.00m, 235.00m, TradeOutcome.Expired, PatternType.DescendingTriangle, PatternDirection.Bearish, "Daily", MarketRegime.Choppy, "Mostly Bearish", "Mixed", "Mixed", "Normal", 38m, 30),

            // ── MORE WINNERS (weekly patterns, breakout volume) ──
            new DemoTrade("NVDA", SignalType.Long, 780.00m, 755.00m, 840.00m, TradeOutcome.HitTarget1, PatternType.BullishEngulfing, PatternDirection.Bullish, "Daily", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Breakout", 68m, 6),
            new DemoTrade("AAPL", SignalType.Long, 195.00m, 190.00m, 208.00m, TradeOutcome.HitTarget1, PatternType.BullishEngulfing, PatternDirection.Bullish, "Weekly", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 55m, 14),

            // ── MORE LOSSES (against the trend) ──
            new DemoTrade("NVDA", SignalType.Short, 760.00m, 785.00m, 720.00m, TradeOutcome.HitStop, PatternType.BearishEngulfing, PatternDirection.Bearish, "4H", MarketRegime.Trending, "Conflicting", "Conflicting", "Bullish Stack", "Low", 45m, 3),
            new DemoTrade("META", SignalType.Short, 530.00m, 548.00m, 500.00m, TradeOutcome.HitStop, PatternType.ShootingStar, PatternDirection.Bearish, "4H", MarketRegime.Trending, "Conflicting", "Conflicting", "Bullish Stack", "Normal", 52m, 4),

            // ── MIXED: some wins on 4H, some losses ──
            new DemoTrade("TSLA", SignalType.Long, 245.00m, 235.00m, 265.00m, TradeOutcome.HitTarget1, PatternType.Hammer, PatternDirection.Bullish, "4H", MarketRegime.MeanReversion, "Mostly Bullish", "Mixed", "Mixed", "Normal", 48m, 5),
            new DemoTrade("GOOGL", SignalType.Long, 172.00m, 167.00m, 182.00m, TradeOutcome.HitTarget1, PatternType.BullishHarami, PatternDirection.Bullish, "Daily", MarketRegime.Trending, "All Bullish", "Aligned", "Bullish Stack", "Normal", 58m, 8),
            new DemoTrade("AMZN", SignalType.Long, 198.00m, 192.00m, 212.00m, TradeOutcome.HitStop, PatternType.BullishEngulfing, PatternDirection.Bullish, "Daily", MarketRegime.VolExpansion, "Mostly Bullish", "Mixed", "Mixed", "Breakout", 65m, 3),
        };

        foreach (var dt in demoTrades)
        {
            var trade = BuildTrade(dt, userId, random);
            trades.Add(trade);
        }

        // Add 3 active (unresolved) trades
        trades.Add(BuildActiveTrade("AAPL", SignalType.Long, 197.50m, 192.00m, 210.00m, PatternType.BullishEngulfing, PatternDirection.Bullish, "Daily", MarketRegime.Trending, "All Bullish", "Aligned", 55m, userId, 2));
        trades.Add(BuildActiveTrade("TSLA", SignalType.Short, 262.00m, 275.00m, 240.00m, PatternType.BearishEngulfing, PatternDirection.Bearish, "Daily", MarketRegime.VolExpansion, "Mostly Bearish", "Mixed", 60m, userId, 1));
        trades.Add(BuildActiveTrade("NVDA", SignalType.Long, 795.00m, 775.00m, 850.00m, PatternType.BullFlag, PatternDirection.Bullish, "Weekly", MarketRegime.Trending, "All Bullish", "Aligned", 70m, userId, 4));

        return trades;
    }

    private static SimulatedTrade BuildTrade(DemoTrade dt, string userId, Random random)
    {
        var isLong = dt.Direction == SignalType.Long;
        var riskPerShare = Math.Abs(dt.Entry - dt.Stop);
        decimal exitPrice;

        if (dt.Outcome == TradeOutcome.HitTarget1)
            exitPrice = dt.Target;
        else if (dt.Outcome == TradeOutcome.HitStop)
            exitPrice = dt.Stop;
        else // Expired — somewhere between entry and a small move
            exitPrice = dt.Entry + (isLong ? 1 : -1) * riskPerShare * 0.3m * (decimal)(random.NextDouble() - 0.3);

        var pnlPerShare = isLong ? exitPrice - dt.Entry : dt.Entry - exitPrice;
        var pnlPct = pnlPerShare / dt.Entry * 100;
        var rMult = riskPerShare > 0 ? pnlPerShare / riskPerShare : 0;
        var shares = (int)(5000m / dt.Entry);

        return new SimulatedTrade
        {
            TradeSignalId = null, // No linked signal for demo data
            Asset = dt.Asset,
            Direction = dt.Direction,
            EntryPrice = dt.Entry,
            StopLoss = dt.Stop,
            Target1 = dt.Target,
            RegimeAtEntry = dt.Regime,
            PatternType = dt.Pattern,
            PatternDirection = dt.PatternDir,
            PatternTimeframe = dt.Timeframe,
            PatternConfidence = dt.Confidence,
            TimeframeAlignment = dt.TFAlignment,
            RegimeAlignment = dt.RegimeAlign,
            MAAlignment = dt.MAAlign,
            VolumeProfile = dt.VolProfile,
            RSIAtEntry = 52m,
            Status = SimulationStatus.Resolved,
            Outcome = dt.Outcome,
            ActivatedAt = DateTime.UtcNow.AddDays(-dt.Days - 5),
            ResolvedAt = DateTime.UtcNow.AddDays(-random.Next(1, 5)),
            DaysHeld = dt.Days,
            MaxExpirationDays = 30,
            ExitPrice = exitPrice,
            PnLPercent = pnlPct,
            PnLDollars = pnlPerShare * shares,
            RMultiple = rMult,
            ShareCount = shares,
            PositionValue = dt.Entry * shares,
            MaxRisk = riskPerShare * shares,
            HighestPriceSeen = isLong ? Math.Max(dt.Entry, exitPrice) + riskPerShare * 0.2m : dt.Entry,
            LowestPriceSeen = isLong ? dt.Entry : Math.Min(dt.Entry, exitPrice) - riskPerShare * 0.2m,
            MaxDrawdownPercent = dt.Outcome == TradeOutcome.HitStop ? riskPerShare / dt.Entry * 100 : riskPerShare * 0.3m / dt.Entry * 100,
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-dt.Days - 5),
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static SimulatedTrade BuildActiveTrade(string asset, SignalType dir, decimal entry, decimal stop, decimal target,
        PatternType pattern, PatternDirection patDir, string tf, MarketRegime regime, string tfAlign, string regimeAlign,
        decimal confidence, string userId, int daysHeld)
    {
        return new SimulatedTrade
        {
            TradeSignalId = null,
            Asset = asset,
            Direction = dir,
            EntryPrice = entry,
            StopLoss = stop,
            Target1 = target,
            RegimeAtEntry = regime,
            PatternType = pattern,
            PatternDirection = patDir,
            PatternTimeframe = tf,
            PatternConfidence = confidence,
            TimeframeAlignment = tfAlign,
            RegimeAlignment = regimeAlign,
            MAAlignment = "Bullish Stack",
            VolumeProfile = "Normal",
            RSIAtEntry = 55m,
            Status = SimulationStatus.Active,
            Outcome = TradeOutcome.Open,
            ActivatedAt = DateTime.UtcNow.AddDays(-daysHeld),
            DaysHeld = daysHeld,
            MaxExpirationDays = 30,
            HighestPriceSeen = entry,
            LowestPriceSeen = entry,
            ShareCount = (int)(5000m / entry),
            PositionValue = entry * (int)(5000m / entry),
            UserId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-daysHeld),
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static async Task BuildPerformanceAggregatesAsync(ApplicationDbContext context, string userId)
    {
        var resolved = await context.SimulatedTrades
            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Resolved && t.PatternType.HasValue)
            .ToListAsync();

        var groups = resolved.GroupBy(t => new { t.PatternType, t.PatternDirection, t.PatternTimeframe, t.RegimeAtEntry });

        foreach (var g in groups)
        {
            var wins = g.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitTarget2);
            var losses = g.Count(t => t.Outcome == TradeOutcome.HitStop);
            var totalDecided = wins + losses;
            var winList = g.Where(t => t.Outcome == TradeOutcome.HitTarget1).ToList();
            var lossList = g.Where(t => t.Outcome == TradeOutcome.HitStop).ToList();
            var aligned = g.Where(t => t.TimeframeAlignment?.Contains("All") == true).ToList();
            var conflicting = g.Where(t => t.TimeframeAlignment == "Conflicting").ToList();
            var breakoutVol = g.Where(t => t.VolumeProfile == "Breakout").ToList();

            var grossWins = winList.Sum(t => Math.Abs(t.PnLPercent ?? 0));
            var grossLosses = lossList.Sum(t => Math.Abs(t.PnLPercent ?? 0));

            var perf = new PatternPerformance
            {
                PatternType = g.Key.PatternType!.Value,
                Direction = g.Key.PatternDirection ?? PatternDirection.Bullish,
                Timeframe = g.Key.PatternTimeframe ?? "Daily",
                Regime = g.Key.RegimeAtEntry,
                TotalTrades = g.Count(),
                Wins = wins,
                Losses = losses,
                Expired = g.Count(t => t.Outcome == TradeOutcome.Expired),
                WinRate = totalDecided > 0 ? (decimal)wins / totalDecided * 100 : 0,
                AvgWinPercent = winList.Any() ? winList.Average(t => t.PnLPercent ?? 0) : 0,
                AvgLossPercent = lossList.Any() ? lossList.Average(t => t.PnLPercent ?? 0) : 0,
                AvgRMultiple = g.Average(t => t.RMultiple ?? 0),
                BestTradePercent = g.Max(t => t.PnLPercent ?? 0),
                WorstTradePercent = g.Min(t => t.PnLPercent ?? 0),
                TotalPnLPercent = g.Sum(t => t.PnLPercent ?? 0),
                ProfitFactor = grossLosses > 0 ? grossWins / grossLosses : grossWins > 0 ? 99 : 0,
                AvgDaysHeld = (decimal)g.Average(t => t.DaysHeld),
                LastTradeDate = DateTime.UtcNow,
                WinRateWhenAligned = aligned.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
                    ? (decimal)aligned.Count(t => t.Outcome == TradeOutcome.HitTarget1) / aligned.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100 : 0,
                TradesWhenAligned = aligned.Count,
                WinRateWhenConflicting = conflicting.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
                    ? (decimal)conflicting.Count(t => t.Outcome == TradeOutcome.HitTarget1) / conflicting.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100 : 0,
                TradesWhenConflicting = conflicting.Count,
                WinRateWithBreakoutVol = breakoutVol.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
                    ? (decimal)breakoutVol.Count(t => t.Outcome == TradeOutcome.HitTarget1) / breakoutVol.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100 : 0,
                TradesWithBreakoutVol = breakoutVol.Count,
                UserId = userId
            };

            context.PatternPerformances.Add(perf);
        }

        await context.SaveChangesAsync();
    }

    private record DemoTrade(string Asset, SignalType Direction, decimal Entry, decimal Stop, decimal Target,
        TradeOutcome Outcome, PatternType Pattern, PatternDirection PatternDir, string Timeframe,
        MarketRegime Regime, string TFAlignment, string RegimeAlign, string MAAlign, string VolProfile,
        decimal Confidence, int Days);
}