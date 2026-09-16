using SmartClaim.Modern.Models;
using SmartClaim.Modern.Services;

// Top-level statements : pas de namespace ni de classe Program visible.
// C'est l'un des changements les plus visibles face au C# "à l'ancienne".
var builder = WebApplication.CreateBuilder(args);

// Injection de dépendances native, sans framework tiers.
builder.Services.AddScoped<IClaimAnalysisService, AzureAiClaimAnalysisService>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// Minimal API : un endpoint = une ligne, pas de contrôleur ni d'attributs [Route]/[HttpPost].
app.MapPost("/api/claims/process", async (ClaimRequest request, IClaimAnalysisService analysisService) =>
{
    if (string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest("La description de la réclamation est obligatoire.");
    }

    var result = await analysisService.AnalyzeAsync(request);
    return Results.Ok(result);
})
.WithName("ProcessClaim");

app.Run();
