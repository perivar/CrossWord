# Remember to invoke this from the parent directory
# docker build -t api . -f .\CrossWord.API\Dockerfile
# OR use docker-compose

# Set build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY CrossWord.Scraper/CrossWord.Scraper.csproj CrossWord.Scraper/
COPY CrossWord.API/CrossWord.API.csproj CrossWord.API/
RUN dotnet restore CrossWord.API/CrossWord.API.csproj

COPY  . .
WORKDIR /src/CrossWord.API

FROM build AS publish
RUN dotnet publish "CrossWord.API.csproj" -c Release -o /app

# Make sure the correct appsetting.json is used since it was overwritten by the appsetting from the CrossWord.Scraper project
COPY CrossWord.API/appsettings.json /app

# Set runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
# ENV ASPNETCORE_URLS http://+:8000
# EXPOSE 8000

# Copy to the app directory 
COPY --from=publish /app .

# Make sure the log file exist
RUN touch crossword_api_error.log

ENTRYPOINT ["dotnet", "CrossWord.API.dll"]
# you can change the entry point from the command line
# e.g.
# docker build -t api . -f .\CrossWord.API\Dockerfile
# docker run -it --rm --entrypoint "bash" api
# ENTRYPOINT ["/bin/bash"]