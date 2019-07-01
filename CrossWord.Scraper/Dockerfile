FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
# Build database
# RUN dotnet ef database update
# Build project
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:2.2
#FROM mcr.microsoft.com/dotnet/core/sdk:2.2

# Install deps + add Chrome Stable + procps for the ps command
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    --no-install-recommends \
    && curl -sSL https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb https://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update && apt-get install -y \
    google-chrome-stable \
    unzip \
    procps \ 
    --no-install-recommends

# Add chrome as a user so we don't need --no-sandbox.
RUN groupadd -r chrome && useradd -r -g chrome -G audio,video chrome \
    && mkdir -p /home/chrome && chown -R chrome:chrome /home/chrome \
    && mkdir -p /opt/google/chrome && chown -R chrome:chrome /opt/google/chrome

# Install ChromeDriver.
RUN wget -N http://chromedriver.storage.googleapis.com/$(curl -sS chromedriver.storage.googleapis.com/LATEST_RELEASE)/chromedriver_linux64.zip -P ~/
RUN unzip ~/chromedriver_linux64.zip -d ~/
RUN rm ~/chromedriver_linux64.zip
RUN mv -f ~/chromedriver /usr/local/bin/chromedriver
RUN chown chrome:chrome /usr/local/bin/chromedriver
RUN chmod 0755 /usr/local/bin/chromedriver

# Purge all the things to keep the container as small as possible
RUN apt-get purge --auto-remove -y curl gnupg unzip \
    && rm -rf /var/lib/apt/lists/*

# Run Chrome non-privileged
USER chrome

# Copy and ensure the app directory is owned by the chrome user 
COPY --chown=chrome:chrome --from=build /app/ /app
WORKDIR /app/out

# Make sure the log file exist
RUN touch crossword_scraper_error.log

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.Scraper.dll"]

# Use this to debug the docker image - keeps it open instead of restarting it
# It is smart to use this in combination with using the SDK version as the runtime image
#CMD tail -f /dev/null
