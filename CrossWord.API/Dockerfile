# Set runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

# Set build image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

WORKDIR /src
COPY CrossWord.API/CrossWord.API.csproj CrossWord.API/
COPY CrossWord.Scraper/CrossWord.Scraper.csproj CrossWord.Scraper/
RUN dotnet restore CrossWord.API/CrossWord.API.csproj

COPY  . .
WORKDIR /src/CrossWord.API

FROM build AS publish
RUN dotnet publish "CrossWord.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CrossWord.API.dll"]
# ENTRYPOINT ["/bin/bash"]