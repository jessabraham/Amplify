namespace Amplify.Application.Common.DTOs.Trading;

public class PortfolioSnapshotDto
{
    public Guid Id { get; set; }
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public int OpenPositions { get; set; }
    public decimal RiskExposurePercent { get; set; }
    public DateTime CreatedAt { get; set; }
}