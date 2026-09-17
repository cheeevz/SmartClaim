# SmartClaim

Système de traitement automatisé de réclamations clients / sinistres — projet vitrine
démontrant une modernisation de code legacy (.NET Framework 4.6) vers une stack
actuelle (.NET 10, Minimal APIs, Azure OpenAI, Docker).

## Structure

- `SmartClaim.Modern/` — l'API .NET 10 réelle du projet (Minimal API, DI native, records, pattern matching, intégration Azure OpenAI)
- `SmartClaim.Legacy/` — un fichier illustratif uniquement, non buildable, pour montrer le style de code "avant"
- `SmartClaim.Tests/` — tests unitaires (xUnit) du service d'analyse
- `Dockerfile` / `.dockerignore` — conteneurisation de l'API

## Différences de syntaxe illustrées (legacy → moderne)

| Aspect | Legacy (.NET 4.6) | Moderne (.NET 10) |
|---|---|---|
| Point d'entrée | `namespace` + `class Program` + `static void Main` | Top-level statements, pas de classe visible |
| Modèles de données | Classe avec getters/setters explicites | `record` immuable, concis |
| Classification | Cascade de `if/else` | `switch` expression avec pattern matching |
| Asynchrone | Appels bloquants (`ExecuteNonQuery`) | `async`/`await` de bout en bout |
| Accès aux données | SQL concaténé à la main (faille d'injection) | Entity Framework Core (LINQ, requêtes paramétrées automatiquement) |
| Routage HTTP | Contrôleurs + attributs | Minimal API (`app.MapPost(...)`) |

## Architecture du service d'analyse

`IClaimAnalysisService` est une abstraction avec deux implémentations :
- `MockClaimAnalysisService` — classification par mots-clés, sans dépendance externe, utilisée pour les tests unitaires
- `AzureAiClaimAnalysisService` — appelle un modèle **gpt-5-mini** déployé sur **Azure OpenAI** pour classifier la réclamation, évaluer son urgence, en extraire un résumé et les entités clés (montant, numéro de contrat)

## Endpoints

- `POST /api/claims/process` — analyse une réclamation (via Azure OpenAI) et persiste le résultat en base
- `GET /api/claims` — liste les réclamations déjà traitées, les plus récentes en premier

## Persistance

Chaque réclamation traitée est enregistrée dans une table `Claims` sur **Azure SQL Database**,
via **Entity Framework Core** (`SmartClaimDbContext`). Le schéma de la base est géré par migrations
EF Core (`SmartClaim.Modern/Migrations/`), appliquées automatiquement au démarrage de l'application.

## État actuel

- [x] Squelette API .NET 10 avec endpoint `POST /api/claims/process`
- [x] Service d'analyse mocké (règles par mots-clés)
- [x] Intégration Azure OpenAI (gpt-5-mini) pour l'analyse réelle du texte
- [x] Dockerfile multi-stage + test du conteneur en local
- [x] Tests unitaires (xUnit) sur le service mocké
- [x] Déploiement Azure Container Apps (image publique accessible)
- [x] Persistance (Azure SQL Database via Entity Framework Core)
- [ ] CI/CD GitHub Actions
- [ ] Mettre à jour le Container App pour connecter la base de données en production (variables d'environnement + migration des restrictions de pare-feu Azure SQL)
- [ ] Amélioration sécurité : déplacer la clé Azure OpenAI vers Azure Key Vault (actuellement en variable d'environnement en clair sur le Container App)

## Lancer le projet en local (avec le SDK .NET installé)

```bash
cd SmartClaim.Modern
dotnet run
```

Puis tester l'endpoint (PowerShell) :

```powershell
$body = @{
    contractNumber = "C-12345"
    customerName   = "Jean Dupont"
    description    = "Colis en retard depuis 2 semaines, remboursement de 45 euros"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/claims/process" -Method Post -Body $body -ContentType "application/json"
```

La configuration Azure OpenAI (endpoint, clé, nom de déploiement) et la chaîne de connexion Azure SQL
(`ConnectionStrings:SmartClaimDb`) se renseignent dans `SmartClaim.Modern/appsettings.Development.json`
(fichier local, jamais commité — voir `.gitignore`).

## Lancer avec Docker

```bash
docker build -t smartclaim-modern .

docker run -p 8080:8080 \
  -e AzureOpenAI__Endpoint="https://<ta-ressource>.openai.azure.com/" \
  -e AzureOpenAI__ApiKey="<ta-cle>" \
  -e AzureOpenAI__DeploymentName="gpt-5-mini" \
  smartclaim-modern
```

L'API est alors accessible sur `http://localhost:8080/api/claims/process`.

## Lancer les tests

```bash
cd SmartClaim.Tests
dotnet test
```

## Déploiement Azure

L'API est déployée et accessible publiquement sur Azure Container Apps :

```
https://smartclaim-api.victorioushill-0c5a38a7.swedencentral.azurecontainerapps.io/api/claims/process
```

**Chaîne de déploiement :**

```
Image Docker locale
  → poussée sur Azure Container Registry (ACR)
  → tirée par Azure Container Apps
  → exposée publiquement via Ingress (port 8080)
```

**Ressources Azure utilisées :**
- `smartclaim-openai-dev` — ressource Azure OpenAI (modèle `gpt-5-mini`)
- `smartclaimacr` — Azure Container Registry (stockage de l'image Docker)
- `smartclaim-api` — Azure Container Apps (exécution du conteneur)

La configuration Azure OpenAI est injectée dans le Container App via variables d'environnement
(`AzureOpenAI__Endpoint`, `AzureOpenAI__ApiKey`, `AzureOpenAI__DeploymentName`), sur le même
principe qu'en local avec `docker run -e`.
