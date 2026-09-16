using System.Text.RegularExpressions;
using SmartClaim.Modern.Models;

namespace SmartClaim.Modern.Services;

// Implémentation "mock" : logique par mots-clés, juste pour avoir un pipeline
// bout-en-bout fonctionnel avant de brancher Azure AI. Volontairement simple.
public partial class MockClaimAnalysisService : IClaimAnalysisService
{
    public Task<ClaimResult> AnalyzeAsync(ClaimRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.Description.ToLowerInvariant();

        // Pattern matching moderne (switch expression) plutôt que des if/else en cascade
        var category = text switch
        {
            var t when t.Contains("livraison") || t.Contains("colis") || t.Contains("retard") => ClaimCategory.Logistique,
            var t when t.Contains("facture") || t.Contains("prélèvement") || t.Contains("remboursement") => ClaimCategory.Facturation,
            var t when t.Contains("panne") || t.Contains("bug") || t.Contains("ne fonctionne pas") => ClaimCategory.Technique,
            _ => ClaimCategory.Autre
        };

        var urgency = text switch
        {
            var t when t.Contains("urgent") || t.Contains("inadmissible") || t.Contains("scandaleux") => UrgencyLevel.Critique,
            var t when t.Contains("rapidement") || t.Contains("déçu") => UrgencyLevel.Elevee,
            _ => UrgencyLevel.Normale
        };

        var montant = ExtraireMontant(request.Description);

        var result = new ClaimResult(
            Category: category,
            Urgency: urgency,
            Summary: $"Réclamation de {request.CustomerName} (contrat {request.ContractNumber}) : {Tronquer(request.Description, 120)}",
            Entities: new ExtractedEntities(montant, request.ContractNumber)
        );

        return Task.FromResult(result);
    }

    private static decimal? ExtraireMontant(string texte)
    {
        var match = MontantRegex().Match(texte);
        if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", "."), out var montant))
            return montant;
        return null;
    }

    private static string Tronquer(string texte, int longueur) =>
        texte.Length <= longueur ? texte : texte[..longueur] + "...";

    [GeneratedRegex(@"(\d+[.,]?\d*)\s?€")]
    private static partial Regex MontantRegex();
}
