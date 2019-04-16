# Remember to invoke this from the parent directory
# see https://stackoverflow.com/questions/51918919/how-to-integrate-npm-install-into-asp-net-core-2-1-docker-build
# docker build -t dbmigrate . -f .\CrossWord.DbMigrate\Dockerfile

# Set build image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

WORKDIR /src
COPY CrossWord.DbMigrate/CrossWord.DbMigrate.csproj CrossWord.DbMigrate/
COPY CrossWord.Scraper/CrossWord.Scraper.csproj CrossWord.Scraper/
RUN dotnet restore CrossWord.DbMigrate/CrossWord.DbMigrate.csproj

COPY  . .
WORKDIR /src/CrossWord.DbMigrate

FROM build AS publish
RUN dotnet publish "CrossWord.DbMigrate.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS base
WORKDIR /app
COPY --from=publish /app .
# ENTRYPOINT ["dotnet", "CrossWord.DbMigrate.dll"]
ENTRYPOINT ["/bin/bash"]
