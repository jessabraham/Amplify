namespace Amplify.Domain.Enumerations;

public enum TradeOutcome
{
    Open,           // Trade is still active, hasn't hit target or stop
    HitTarget1,     // Price reached Target 1
    HitTarget2,     // Price reached Target 2
    HitStop,        // Price hit stop loss
    Expired,        // Trade expired without hitting either level (time-based)
    ManualClose     // User closed the trade manually
}

public enum SimulationStatus
{
    Pending,        // Signal saved but simulation not started yet
    Active,         // Being actively tracked
    Resolved,       // Final outcome determined
    Cancelled       // User cancelled before resolution
}