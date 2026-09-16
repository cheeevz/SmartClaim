using SmartClaim.Modern.Models;

namespace SmartClaim.Modern.Services;

// Abstraction volontaire : en étape 2, on ajoutera une implémentation
// AzureAiClaimAnalysisService qui appelle Azure OpenAI / Text Analytics,
// sans rien changer côté endpoints (Dependency Injection native de .NET).
public interface IClaimAnalysisService
{
    Task<ClaimResult> AnalyzeAsync(ClaimRequest request, CancellationToken cancellationToken = default);
}
