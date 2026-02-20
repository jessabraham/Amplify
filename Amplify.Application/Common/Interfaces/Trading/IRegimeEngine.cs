using Amplify.Application.Common.DTOs.Market;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface IRegimeEngine
{
    /// <summary>
    /// Classify current market regime from a computed feature vector.
    /// Returns regime type, confidence (0-100), and rationale.
    /// </summary>
    RegimeResultDto DetectRegime(FeatureVectorDto features);
}