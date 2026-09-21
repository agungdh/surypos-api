# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files dulu untuk layer caching restore
COPY SuryPosApp.slnx ./
COPY src/SuryPos.Api/SuryPos.Api.csproj src/SuryPos.Api/
COPY src/SuryPos.Data/SuryPos.Data.csproj src/SuryPos.Data/
COPY src/SuryPos.Domain/SuryPos.Domain.csproj src/SuryPos.Domain/
COPY src/SuryPos.Service/SuryPos.Service.csproj src/SuryPos.Service/
RUN dotnet restore SuryPosApp.slnx

# Copy sisa source lalu publish
COPY . ./
RUN dotnet publish src/SuryPos.Api/SuryPos.Api.csproj -c Release -o /app/publish

# Runtime stage (chiseled: minimal, no shell, non-root)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
USER app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SuryPos.Api.dll"]
