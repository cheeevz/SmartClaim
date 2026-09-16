using SmartClaim.Modern.Models;
using SmartClaim.Modern.Services;
using Xunit;

namespace SmartClaim.Tests;

public class MockClaimAnalysisServiceTests
{
    private readonly MockClaimAnalysisService _service = new();

    // [Theory] + [InlineData] : on fait tourner le même test avec plusieurs jeux de données,
    // plutôt que de dupliquer un [Fact] par cas. Pratique pour couvrir plusieurs mots-clés d'un coup.
    [Theory]
    [InlineData("Mon colis n'est jamais arrivé", ClaimCategory.Logistique)]
    [InlineData("Livraison en retard depuis 3 jours", ClaimCategory.Logistique)]
    [InlineData("Je conteste ma facture de ce mois-ci", ClaimCategory.Facturation)]
    [InlineData("Je veux un remboursement immédiat", ClaimCategory.Facturation)]
    [InlineData("L'application est en panne depuis ce matin", ClaimCategory.Technique)]
    [InlineData("Bonjour, j'ai une question générale", ClaimCategory.Autre)]
    public async Task AnalyzeAsync_DetecteLaBonneCategorie(string description, ClaimCategory categorieAttendue)
    {
        var request = new ClaimRequest("C-001", "Client Test", description);

        var result = await _service.AnalyzeAsync(request);

        Assert.Equal(categorieAttendue, result.Category);
    }

    [Theory]
    [InlineData("C'est urgent, il faut agir maintenant", UrgencyLevel.Critique)]
    [InlineData("C'est scandaleux, inadmissible !", UrgencyLevel.Critique)]
    [InlineData("Je suis déçu du service", UrgencyLevel.Eleve)]
    [InlineData("Petite question sans importance", UrgencyLevel.Normal)]
    public async Task AnalyzeAsync_DetecteLeBonNiveauDurgence(string description, UrgencyLevel urgenceAttendue)
    {
        var request = new ClaimRequest("C-002", "Client Test", description);

        var result = await _service.AnalyzeAsync(request);

        Assert.Equal(urgenceAttendue, result.Urgency);
    }

    [Fact]
    public async Task AnalyzeAsync_ExtraitLeMontantEnEuros()
    {
        var request = new ClaimRequest("C-003", "Client Test", "Je demande un remboursement de 45€ sous 15 jours");

        var result = await _service.AnalyzeAsync(request);

        Assert.Equal(45m, result.Entities.MontantDetecte);
    }

    [Fact]
    public async Task AnalyzeAsync_RetourneMontantNullSiAucunMontantMentionne()
    {
        var request = new ClaimRequest("C-004", "Client Test", "Mon colis est en retard, sans autre précision");

        var result = await _service.AnalyzeAsync(request);

        Assert.Null(result.Entities.MontantDetecte);
    }

    [Fact]
    public async Task AnalyzeAsync_ReporteLeNumeroDeContratDansLesEntites()
    {
        var request = new ClaimRequest("C-12345", "Client Test", "Un souci quelconque");

        var result = await _service.AnalyzeAsync(request);

        Assert.Equal("C-12345", result.Entities.NumeroContratDetecte);
    }

    [Fact]
    public async Task AnalyzeAsync_TronqueLeResumeSiLaDescriptionEstLongue()
    {
        var descriptionLongue = new string('a', 200);
        var request = new ClaimRequest("C-005", "Client Test", descriptionLongue);

        var result = await _service.AnalyzeAsync(request);

        Assert.Contains("...", result.Summary);
    }
}
