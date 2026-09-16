// ⚠️ Fichier illustratif uniquement (style .NET Framework 4.6 / C# ancienne génération).
// Volontairement non intégré à un projet buildable : il sert de point de comparaison
// avec SmartClaim.Modern dans le README, pas de code de production.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SmartClaim.Legacy
{
    // Pas de record, pas de propriétés auto modernes : classe classique avec getters/setters.
    public class ClaimData
    {
        private string _contractNumber;
        private string _customerName;
        private string _description;

        public string ContractNumber { get { return _contractNumber; } set { _contractNumber = value; } }
        public string CustomerName { get { return _customerName; } set { _customerName = value; } }
        public string Description { get { return _description; } set { _description = value; } }
    }

    public class ClaimProcessor
    {
        // Pas d'async/await : tout est synchrone et bloquant.
        public string ProcessClaim(ClaimData claim)
        {
            // Validation manuelle, pas de data annotations
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            if (String.IsNullOrEmpty(claim.Description))
            {
                throw new ArgumentException("La description ne peut pas etre vide.");
            }

            // Classification "a la main" avec des if/else en cascade
            string category;
            string descLower = claim.Description.ToLower();

            if (descLower.Contains("livraison") || descLower.Contains("colis"))
            {
                category = "Logistique";
            }
            else if (descLower.Contains("facture") || descLower.Contains("remboursement"))
            {
                category = "Facturation";
            }
            else if (descLower.Contains("panne") || descLower.Contains("bug"))
            {
                category = "Technique";
            }
            else
            {
                category = "Autre";
            }

            // Ecriture directe en base, requete SQL brute concatenee (mauvaise pratique
            // volontairement conservee ici pour illustrer le "avant")
            SaveToDatabase(claim, category);

            return category;
        }

        private void SaveToDatabase(ClaimData claim, string category)
        {
            string connectionString = "Data Source=SERVER;Initial Catalog=Claims;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Concatenation de chaine pour construire la requete : a eviter (injection SQL),
                // mais representatif de code legacy reel.
                string query = "INSERT INTO Claims (ContractNumber, CustomerName, Description, Category) VALUES ('" +
                    claim.ContractNumber + "', '" + claim.CustomerName + "', '" + claim.Description + "', '" + category + "')";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
