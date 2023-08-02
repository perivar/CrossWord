# Remember to invoke this from the parent directory
# docker build -t web . -f .\CrossWord.Web\Dockerfile
# OR use docker-compose

# Set runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
# ENV ASPNETCORE_URLS http://+:5000
# EXPOSE 5000

# Add ASP.NET Core 2.2
# COPY --from=mcr.microsoft.com/dotnet/core/aspnet:2.2 /usr/share/dotnet /usr/share/dotnet

# .NET Core SDK and Node.js images
# https://github.com/DotNet-Docker-Images/dotnet-nodejs-docker/blob/master/Dockerfile
FROM dotnetimages/microsoft-dotnet-core-sdk-nodejs:7.0_19.x AS build

# Add .NET Core 2.2 SDK
# COPY --from=mcr.microsoft.com/dotnet/core/sdk:2.2 /usr/share/dotnet/sdk /usr/share/dotnet/sdk

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
