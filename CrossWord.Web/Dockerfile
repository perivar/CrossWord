# Invoke this from the parent directory
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /src

COPY CrossWord.Web/ CrossWord.Web/
COPY CrossWord.Scraper/ CrossWord.Scraper/
COPY CrossWord/ CrossWord/
WORKDIR /src/CrossWord.Web
RUN dotnet restore

# Build database
# RUN dotnet ef database update
# Build project
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
WORKDIR /.
COPY --from=build-env /src/CrossWord.Web/out .

ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.Web.dll"]
