namespace Amplify.Application.Common.DTOs.Trading;

public class PortfolioSummaryDto
{
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal RiskExposurePercent { get; set; }
    public int OpenPositionCount { get; set; }
    public int ClosedPositionCount { get; set; }

    public List<PositionDto> OpenPositions { get; set; } = [];
    public List<AssetAllocationDto> AssetAllocation { get; set; } = [];
}

public class AssetAllocationDto
{
    public string AssetClass { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public decimal Percentage { get; set; }
    public int PositionCount { get; set; }
}