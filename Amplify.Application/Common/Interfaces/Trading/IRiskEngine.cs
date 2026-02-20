using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface IRiskEngine
{
    /// <summary>
    /// Calculate full risk assessment: position sizing, risk/reward, Kelly criterion,
    /// and validate against portfolio risk limits.
    /// </summary>
    Result<RiskAssessmentDto> CalculateRisk(RiskInputDto input);
}