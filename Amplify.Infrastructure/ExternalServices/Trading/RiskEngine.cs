using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Amplify.Infrastructure.ExternalServices.Trading;

/// <summary>
/// Risk Engine — calculates position sizing, risk/reward ratios, Kelly criterion,
/// and enforces portfolio risk limits from configuration.
///
/// Position sizing methods:
///   1. Fixed-percent: Risk a fixed % of portfolio per trade
///   2. Kelly criterion: Optimal sizing based on win rate and payoff ratio
///
/// Risk checks:
///   - Max risk per trade (from config)
///   - Max position size (from config)
///   - Max position as % of portfolio
///   - Minimum risk/reward ratio warning
/// </summary>
public class RiskEngine : IRiskEngine
{
    private readonly decimal _defaultRiskPercent;
    private readonly decimal _maxRiskPercent;
    private readonly decimal _maxPositionSize;

    public RiskEngine(IConfiguration config)
    {
        _defaultRiskPercent = decimal.TryParse(config["Risk:DefaultRiskPercent"], out var dr) ? dr : 2.0m;
        _maxRiskPercent = decimal.TryParse(config["Risk:MaxRiskPercent"], out var mr) ? mr : 5.0m;
        _maxPositionSize = decimal.TryParse(config["Risk:MaxPositionSize"], out var mp) ? mp : 50_000m;
    }

    public Result<RiskAssessmentDto> CalculateRisk(RiskInputDto input)
    {
        // ── Validation ───────────────────────────────────────────────
        if (input.EntryPrice <= 0)
            return Result<RiskAssessmentDto>.Failure("Entry price must be positive.");
        if (input.StopLoss <= 0)
            return Result<RiskAssessmentDto>.Failure("Stop loss must be positive.");
        if (input.Target1 <= 0)
            return Result<RiskAssessmentDto>.Failure("Target 1 must be positive.");
        if (input.PortfolioSize < 1_000)
            return Result<RiskAssessmentDto>.Failure("Portfolio size must be at least $1,000.");

        // For long: stop must be below entry, target above entry
        // For short: stop must be above entry, target below entry
        if (!input.IsShort)
        {
            if (input.StopLoss >= input.EntryPrice)
                return Result<RiskAssessmentDto>.Failure("Long trade: stop loss must be below entry price.");
            if (input.Target1 <= input.EntryPrice)
                return Result<RiskAssessmentDto>.Failure("Long trade: target must be above entry price.");
        }
        else
        {
            if (input.StopLoss <= input.EntryPrice)
                return Result<RiskAssessmentDto>.Failure("Short trade: stop loss must be above entry price.");
            if (input.Target1 >= input.EntryPrice)
                return Result<RiskAssessmentDto>.Failure("Short trade: target must be below entry price.");
        }

        var riskPercent = input.RiskPercent ?? _defaultRiskPercent;
        var result = new RiskAssessmentDto
        {
            Symbol = input.Symbol,
            EntryPrice = input.EntryPrice,
            StopLoss = input.StopLoss,
            Target1 = input.Target1,
            Target2 = input.Target2,
            PortfolioSize = input.PortfolioSize,
            RiskPercent = riskPercent,
            Warnings = [],
            Violations = []
        };

        // ── Risk per share ───────────────────────────────────────────
        result.RiskPerShare = Math.Abs(input.EntryPrice - input.StopLoss);
        result.RewardPerShare1 = Math.Abs(input.Target1 - input.EntryPrice);
        result.RewardPerShare2 = input.Target2.HasValue
            ? Math.Abs(input.Target2.Value - input.EntryPrice)
            : null;

        // ── Risk/Reward Ratios ───────────────────────────────────────
        result.RiskRewardRatio1 = result.RiskPerShare > 0
            ? Math.Round(result.RewardPerShare1 / result.RiskPerShare, 2)
            : 0;
        result.RiskRewardRatio2 = result.RewardPerShare2.HasValue && result.RiskPerShare > 0
            ? Math.Round(result.RewardPerShare2.Value / result.RiskPerShare, 2)
            : null;

        // ── Position Sizing (Fixed-Percent Method) ───────────────────
        result.RiskAmountDollars = Math.Round(input.PortfolioSize * riskPercent / 100m, 2);

        if (result.RiskPerShare > 0)
        {
            result.ShareCount = Math.Floor(result.RiskAmountDollars / result.RiskPerShare);
            result.PositionSize = Math.Round(result.ShareCount * input.EntryPrice, 2);
        }

        result.PositionValue = result.PositionSize;
        result.PositionPercentOfPortfolio = input.PortfolioSize > 0
            ? Math.Round(result.PositionSize / input.PortfolioSize * 100m, 2)
            : 0;

        // ── Potential Outcomes ────────────────────────────────────────
        result.MaxLoss = Math.Round(result.ShareCount * result.RiskPerShare, 2);
        result.PotentialGain1 = Math.Round(result.ShareCount * result.RewardPerShare1, 2);
        result.PotentialGain2 = result.RewardPerShare2.HasValue
            ? Math.Round(result.ShareCount * result.RewardPerShare2.Value, 2)
            : null;

        // ── Kelly Criterion ──────────────────────────────────────────
        if (input.WinRate.HasValue && input.WinRate > 0 && input.WinRate < 100
            && result.RiskRewardRatio1 > 0)
        {
            var w = input.WinRate.Value / 100m;
            var rr = result.RiskRewardRatio1;
            // Kelly % = W - (1-W)/R
            var kelly = w - (1m - w) / rr;
            result.KellyPercent = Math.Round(Math.Max(kelly * 100m, 0), 2);
            result.KellyPositionSize = Math.Round(input.PortfolioSize * Math.Max(kelly, 0), 2);
        }

        // ── Risk Checks ──────────────────────────────────────────────
        result.PassesRiskCheck = true;

        // Check: risk percent exceeds max
        if (riskPercent > _maxRiskPercent)
        {
            result.Violations.Add($"Risk of {riskPercent}% exceeds max allowed {_maxRiskPercent}%.");
            result.PassesRiskCheck = false;
        }

        // Check: position size exceeds max
        if (result.PositionSize > _maxPositionSize)
        {
            result.Violations.Add($"Position size ${result.PositionSize:N0} exceeds max ${_maxPositionSize:N0}.");
            result.PassesRiskCheck = false;
        }

        // Check: position > 25% of portfolio
        if (result.PositionPercentOfPortfolio > 25)
        {
            result.Violations.Add($"Position is {result.PositionPercentOfPortfolio}% of portfolio (max 25%).");
            result.PassesRiskCheck = false;
        }

        // Warning: R:R below 1.5
        if (result.RiskRewardRatio1 < 1.5m)
            result.Warnings.Add($"Risk/Reward of {result.RiskRewardRatio1}:1 is below recommended 1.5:1.");

        // Warning: R:R below 1.0
        if (result.RiskRewardRatio1 < 1.0m)
            result.Warnings.Add("Negative expectancy — reward is less than risk.");

        // Warning: very small position
        if (result.ShareCount < 1)
            result.Warnings.Add("Calculated share count is 0 — risk budget too small for this price level.");

        // Warning: Kelly suggests 0 or negative
        if (result.KellyPercent.HasValue && result.KellyPercent <= 0)
            result.Warnings.Add("Kelly criterion suggests no position — negative edge.");

        return Result<RiskAssessmentDto>.Success(result);
    }
}