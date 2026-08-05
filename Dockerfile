# Etapa 1: compilar a API usando o SDK completo do .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Barbearia.Api/ ./src/Barbearia.Api/
RUN dotnet publish src/Barbearia.Api/Barbearia.Api.csproj -c Release -o /app/publish

# Etapa 2: imagem final, bem mais leve, só com o necessário para RODAR (sem o SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Barbearia.Api.dll"]
