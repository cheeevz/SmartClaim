using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using SmartClaim.Modern.Models;

namespace SmartClaim.Modern.Services;

// Remplace MockClaimAnalysisService par un vrai appel à Azure OpenAI (gpt-5-mini).
// L'abstraction IClaimAnalysisService fait qu'on n'a rien à toucher côté Program.cs
// ou côté endpoint : seule l'implémentation injectée change.
public class AzureAiClaimAnalysisService : IClaimAnalysisService
{
    private readonly ChatClient _chatClient;

    public AzureAiClaimAnalysisService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint manquant dans la configuration.");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey manquant dans la configuration.");
        var deploymentName = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName manquant dans la configuration.");

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    public async Task<ClaimResult> AnalyzeAsync(ClaimRequest request, CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
            Tu es un assistant qui analyse des réclamations clients pour une compagnie d'assurance.
            Réponds UNIQUEMENT avec un objet JSON valide, sans texte autour, au format exact suivant :
            {
              "category": "Logistique" | "Facturation" | "Technique" | "Autre",
              "urgency": "Faible" | "Normale" | "Elevee" | "Critique",
              "summary": "résumé en une phrase de la réclamation",
              "montantDetecte": nombre décimal ou null si aucun montant mentionné
            }
            """;

        var userPrompt = $"""
            Réclamation du client {request.CustomerName} (contrat {request.ContractNumber}) :
            "{request.Description}"
            """;

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        // Pas de limite explicite de tokens ici : la version actuelle du SDK Azure.AI.OpenAI
        // sérialise MaxOutputTokenCount en "max_tokens", paramètre non supporté par gpt-5-mini
        // (qui attend "max_completion_tokens"). On laisse le modèle utiliser sa limite par défaut.
        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var rawJson = completion.Content[0].Text;

        return ParseModelResponse(rawJson, request.ContractNumber);
    }

    private static ClaimResult ParseModelResponse(string rawJson, string contractNumber)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var category = Enum.TryParse<ClaimCategory>(root.GetProperty("category").GetString(), out var cat)
            ? cat
            : ClaimCategory.Autre;

        var urgency = Enum.TryParse<UrgencyLevel>(root.GetProperty("urgency").GetString(), out var urg)
            ? urg
            : UrgencyLevel.Normal;

        var summary = root.GetProperty("summary").GetString() ?? "Résumé indisponible.";

        decimal? montant = root.TryGetProperty("montantDetecte", out var montantProp) && montantProp.ValueKind == JsonValueKind.Number
            ? montantProp.GetDecimal()
            : null;

        return new ClaimResult(category, urgency, summary, new ExtractedEntities(montant, contractNumber));
    }
}