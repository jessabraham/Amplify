using Amplify.Domain.Enumerations;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.AI;

public class AIAnalytic : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? TradeSignalId { get; set; }
    public TradeSignal? TradeSignal { get; set; }

    public AIAnalysisType AnalysisType { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string PromptSent { get; set; } = string.Empty;
    public string ResponseReceived { get; set; } = string.Empty;

    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double ResponseTimeMs { get; set; }
}