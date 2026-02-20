namespace Amplify.Application.Common.DTOs.Trading;

public class RiskAssessmentDto
{
    // Input echo
    public string Symbol { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }
    public decimal PortfolioSize { get; set; }
    public decimal RiskPercent { get; set; }

    // Position sizing
    public decimal RiskAmountDollars { get; set; }
    public decimal PositionSize { get; set; }
    public decimal ShareCount { get; set; }
    public decimal PositionValue { get; set; }
    public decimal PositionPercentOfPortfolio { get; set; }

    // Risk/Reward
    public decimal RiskPerShare { get; set; }
    public decimal RewardPerShare1 { get; set; }
    public decimal? RewardPerShare2 { get; set; }
    public decimal RiskRewardRatio1 { get; set; }
    public decimal? RiskRewardRatio2 { get; set; }

    // Potential outcomes
    public decimal MaxLoss { get; set; }
    public decimal PotentialGain1 { get; set; }
    public decimal? PotentialGain2 { get; set; }

    // Kelly criterion
    public decimal? KellyPercent { get; set; }
    public decimal? KellyPositionSize { get; set; }

    // Validation
    public bool PassesRiskCheck { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Violations { get; set; } = [];
}