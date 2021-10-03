# Remember to invoke this from the parent directory
# docker build -t web . -f .\CrossWord.Web\Dockerfile
# OR use docker-compose

# Set runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
# ENV ASPNETCORE_URLS http://+:5000
# EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

# install NodeJS 13.x
# see https://github.com/nodesource/distributions/blob/master/README.md#deb
RUN apt-get update -yq 
RUN apt-get upgrade -yq 
RUN apt-get install build-essential -yq
RUN apt-get install curl gnupg -yq
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs
RUN echo "NODE Version:" && node --version
RUN echo "NPM Version:" && npm --version
RUN rm -rf /var/lib/apt/lists/*

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
