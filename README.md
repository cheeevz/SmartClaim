# SmartClaim

Système de traitement automatisé de réclamations clients / sinistres — projet vitrine
démontrant une modernisation de code legacy (.NET Framework 4.6) vers une stack
actuelle (.NET 9, Minimal APIs, puis services IA Azure).

## Structure

- `SmartClaim.Modern/` — l'API .NET 9 réelle du projet (Minimal API, DI native, records, pattern matching)
- `SmartClaim.Legacy/` — un fichier illustratif uniquement, non buildable, pour montrer le style de code "avant"

## Différences de syntaxe illustrées (legacy → moderne)

| Aspect | Legacy (.NET 4.6) | Moderne (.NET 9) |
|---|---|---|
| Point d'entrée | `namespace` + `class Program` + `static void Main` | Top-level statements, pas de classe visible |
| Modèles de données | Classe avec getters/setters explicites | `record` immuable, concis |
| Classification | Cascade de `if/else` | `switch` expression avec pattern matching |
| Asynchrone | Appels bloquants (`ExecuteNonQuery`) | `async`/`await` de bout en bout |
| Accès aux données | SQL concaténé à la main (faille d'injection) | À faire en étape suivante : requêtes paramétrées / EF Core |
| Routage HTTP | Contrôleurs + attributs | Minimal API (`app.MapPost(...)`) |

## État actuel

- [x] Squelette API .NET 9 avec endpoint `POST /api/claims/process`
- [x] Service d'analyse mocké (règles par mots-clés)
- [ ] Intégration Azure AI (OpenAI / Text Analytics) pour l'analyse réelle du texte
- [ ] Persistance (Azure SQL ou Table Storage)
- [ ] Dockerfile + déploiement Azure Container Apps
- [ ] CI/CD GitHub Actions
- [ ] Tests unitaires

## Lancer le projet (chez toi, avec le SDK .NET installé)

```bash
cd SmartClaim.Modern
dotnet run
```

Puis tester l'endpoint :

```bash
curl -X POST http://localhost:5000/api/claims/process \
  -H "Content-Type: application/json" \
  -d '{"contractNumber":"C-12345","customerName":"Jean Dupont","description":"Colis en retard depuis 2 semaines, c'\''est inadmissible, je veux un remboursement de 45€"}'
```
