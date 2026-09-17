using System.ComponentModel.DataAnnotations.Schema;

namespace SmartClaim.Modern.Data;

// Entité de persistance : distincte de ClaimResult (le DTO retourné par l'API).
// On garde les deux séparés volontairement : ClaimRecord peut évoluer selon les besoins
// de stockage (ajout d'un Id, d'une date...) sans casser le contrat de l'API.
public class ClaimRecord
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MontantDetecte { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}