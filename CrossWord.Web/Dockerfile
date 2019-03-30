
# Remember to invoke this from the parent directory
# see https://stackoverflow.com/questions/51918919/how-to-integrate-npm-install-into-asp-net-core-2-1-docker-build

# Set runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

# Install node-js and npm
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_11.x |  bash - \
    && apt-get install nodejs -yq \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY CrossWord.Web/CrossWord.Web.csproj CrossWord.Web/
COPY CrossWord.Scraper/CrossWord.Scraper.csproj CrossWord.Scraper/
COPY CrossWord/CrossWord.csproj CrossWord/
RUN dotnet restore CrossWord.Web/CrossWord.Web.csproj

COPY  . .
WORKDIR /src/CrossWord.Web

# Install dependicies for node
RUN npm install

FROM build AS publish
RUN dotnet publish "CrossWord.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CrossWord.Web.dll"]
