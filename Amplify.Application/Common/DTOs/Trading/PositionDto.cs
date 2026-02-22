using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.DTOs.Trading;

public class PositionDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }

    public decimal EntryPrice { get; set; }
    public decimal Quantity { get; set; }
    public DateTime EntryDateUtc { get; set; }

    public decimal? ExitPrice { get; set; }
    public DateTime? ExitDateUtc { get; set; }

    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }

    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal? ReturnPercent { get; set; }

    public PositionStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsAiGenerated { get; set; }
    public Guid? TradeSignalId { get; set; }
    public DateTime CreatedAt { get; set; }
}