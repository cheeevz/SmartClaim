namespace SmartClaim.Modern.Models;

// Requête entrante : ce que le client (ou un vieux formulaire, dans le scénario "legacy")
// envoie pour signaler une réclamation ou un sinistre.
public record ClaimRequest(
    string ContractNumber,
    string CustomerName,
    string Description
);

// Catégories métier possibles. En C# moderne : enum classique, mais pattern-matché
// proprement grâce aux switch expressions (voir ClaimAnalysisService).
public enum ClaimCategory
{
    Logistique,
    Facturation,
    Technique,
    Autre
}

public enum UrgencyLevel
{
    Faible,
    Normale,
    Elevee,
    Critique
}

// Entités extraites du texte brut par le service d'analyse.
public record ExtractedEntities(
    decimal? MontantDetecte,
    string? NumeroContratDetecte
);

// Résultat retourné par l'API après analyse de la réclamation.
public record ClaimResult(
    ClaimCategory Category,
    UrgencyLevel Urgency,
    string Summary,
    ExtractedEntities Entities
);
