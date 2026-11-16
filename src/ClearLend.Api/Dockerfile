FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClearLend.slnx Directory.Build.props Directory.Packages.props global.json ./
COPY src ./src
COPY tests ./tests

RUN dotnet restore src/ClearLend.Api/ClearLend.Api.csproj
RUN dotnet publish src/ClearLend.Api/ClearLend.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClearLend.Api.dll"]
