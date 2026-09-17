using Microsoft.EntityFrameworkCore;
using SmartClaim.Modern.Data;
using SmartClaim.Modern.Models;
using SmartClaim.Modern.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IClaimAnalysisService, AzureAiClaimAnalysisService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Connexion à Azure SQL via la chaîne de connexion définie en config
// (appsettings.Development.json en local, variable d'environnement en conteneur).
builder.Services.AddDbContext<SmartClaimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartClaimDb")));

var app = builder.Build();

// Applique automatiquement les migrations en attente au démarrage.
// Pratique pour un projet vitrine (pas de commande manuelle à lancer après déploiement),
// à remplacer par une étape de déploiement dédiée dans un vrai contexte de production.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartClaimDbContext>();
    await db.Database.MigrateAsync();
}

app.MapPost("/api/claims/process", async (
    ClaimRequest request,
    IClaimAnalysisService analysisService,
    SmartClaimDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest("La description de la réclamation est obligatoire.");
    }

    var result = await analysisService.AnalyzeAsync(request);

    var record = new ClaimRecord
    {
        ContractNumber = request.ContractNumber,
        CustomerName = request.CustomerName,
        Description = request.Description,
        Category = result.Category.ToString(),
        Urgency = result.Urgency.ToString(),
        Summary = result.Summary,
        MontantDetecte = result.Entities.MontantDetecte,
        ProcessedAtUtc = DateTime.UtcNow
    };

    db.Claims.Add(record);
    await db.SaveChangesAsync();

    return Results.Ok(result);
})
.WithName("ProcessClaim");

// Liste les réclamations déjà traitées, les plus récentes en premier.
app.MapGet("/api/claims", async (SmartClaimDbContext db) =>
{
    var claims = await db.Claims
        .OrderByDescending(c => c.ProcessedAtUtc)
        .ToListAsync();

    return Results.Ok(claims);
})
.WithName("ListClaims");

app.Run();
