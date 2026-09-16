# --- Étape 1 : build ---
# On utilise l'image SDK complète (lourde) uniquement pour compiler,
# elle ne fera pas partie de l'image finale.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copier uniquement le .csproj d'abord : Docker met cette étape en cache
# tant que le .csproj ne change pas, donc "dotnet restore" ne se refait
# pas à chaque modification du code source.
COPY SmartClaim.Modern/SmartClaim.Modern.csproj ./SmartClaim.Modern/
RUN dotnet restore SmartClaim.Modern/SmartClaim.Modern.csproj

# Copier le reste du code source et compiler
COPY SmartClaim.Modern/ ./SmartClaim.Modern/
WORKDIR /src/SmartClaim.Modern
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Étape 2 : exécution ---
# Image runtime uniquement (pas de SDK) : beaucoup plus légère,
# c'est celle qui sera réellement déployée.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Bonne pratique sécurité : ne pas exécuter le conteneur en root
USER app

COPY --from=build /app/publish .

# Le conteneur écoute sur le port 8080 par défaut sur les images ASP.NET récentes
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmartClaim.Modern.dll"]
