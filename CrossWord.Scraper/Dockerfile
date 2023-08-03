FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM perivar/dotnet7runtime-chromedriver:latest

# Copy and ensure the app directory is owned by the chrome user 
COPY --from=build /app/ /app
WORKDIR /app/out

# Make sure the log file exist
RUN touch crossword_scraper_error.log

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.Scraper.dll"]

# Use this to debug the docker image - keeps it open instead of restarting it
# It is smart to use this in combination with using the SDK version as the runtime image
# CMD tail -f /dev/null
