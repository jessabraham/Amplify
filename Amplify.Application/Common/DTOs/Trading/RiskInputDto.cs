namespace Amplify.Application.Common.DTOs.Trading;

public class RiskInputDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }
    public decimal PortfolioSize { get; set; }
    public decimal? RiskPercent { get; set; }
    public decimal? PositionBudget { get; set; }
    public bool IsShort { get; set; }

    /// <summary>
    /// Optional historical win rate (0-100) for Kelly criterion calculation.
    /// </summary>
    public decimal? WinRate { get; set; }
}