FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0
# When debugging the docker image it is helpful to use the sdk instead of the runtime
# FROM mcr.microsoft.com/dotnet/sdk:7.0

# Add .NET Core 2.2
# uncomment if you need the SDK also (unlikely):
# COPY --from=mcr.microsoft.com/dotnet/core/sdk:2.2 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=mcr.microsoft.com/dotnet/core/runtime:2.2 /usr/share/dotnet /usr/share/dotnet

# Install Chrome Stable and the related version of ChromeDriver
# https://gist.github.com/varyonic/dea40abcf3dd891d204ef235c6e8dd79
# https://hub.docker.com/r/blueimp/chromedriver/dockerfile
# https://gerg.dev/2021/06/making-chromedriver-and-chrome-versions-match-in-a-docker-image/
# https://github.com/Yleisradio/docker-robotframework/blob/master/robotframework/Dockerfile
# https://gist.github.com/Faq/8821c5fd18dd01da4f80d7435158096d
RUN set -ex; \
    apt-get update -y; \
    # install required utils and procps for the ps command
    apt-get install -y gnupg wget curl unzip jq procps --no-install-recommends; \
    wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | \
    gpg --no-default-keyring --keyring gnupg-ring:/etc/apt/trusted.gpg.d/google.gpg --import; \
    chmod 644 /etc/apt/trusted.gpg.d/google.gpg; \
    echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list; \
    apt-get update -y; \
    # install latest google chrome
    apt-get install -y google-chrome-stable --no-install-recommends; \
    CHROME_VERSION=$(google-chrome --version); \
    CHROME_MAJOR_VERSION=$(google-chrome --version | sed 's/Google Chrome \([0-9]*\).*/\1/g'); \
    # CHROMEDRIVER_VERSION=$(curl -s "https://chromedriver.storage.googleapis.com/RELEASE_${CHROME_MAJOR_VERSION}"); \
    # CHROMEDRIVER_URL="https://chromedriver.storage.googleapis.com/$CHROMEDRIVER_VERSION/chromedriver_linux64.zip"; \
    CHROME_JSON=$(curl -s https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json | jq '.channels.Stable'); \
    CHROMEDRIVER_VERSION=$(echo "$CHROME_JSON" | jq -r '.version'); \
    CHROMEDRIVER_URL=$(echo "$CHROME_JSON" | jq -r '.downloads.chromedriver[] | select(.platform == "linux64") | .url'); \
    echo "**********************************************************"; \
    echo "chrome version: $CHROME_VERSION"; \
    echo "chrome major version: $CHROME_MAJOR_VERSION"; \
    echo "desired chromedriver version: $CHROMEDRIVER_VERSION"; \
    echo "desired chromedriver url: $CHROMEDRIVER_URL"; \
    wget -q --continue -P /tmp $CHROMEDRIVER_URL; \
    unzip /tmp/chromedriver-linux64.zip -d /tmp; \
    mv -f /tmp/chromedriver-linux64/chromedriver /usr/local/bin/chromedriver; \
    CHROMEDRIVER_MAJOR_VERSION=$(chromedriver --version | sed 's/ChromeDriver \([0-9]*\).*/\1/g'); \
    echo "actual chromedriver version: $CHROMEDRIVER_MAJOR_VERSION"; \
    echo "**********************************************************"; \
    if [ $CHROME_MAJOR_VERSION != $CHROMEDRIVER_MAJOR_VERSION ]; then echo "VERSION MISMATCH"; exit 1; fi; \
    # Purge all the things to keep the container as small as possible
    apt-get purge --auto-remove -y curl gnupg unzip jq; \
    rm -rf \
        /tmp/* \
        /usr/share/doc/* \
        /var/cache/* \
        /var/lib/apt/lists/* \
        /var/tmp/*;

# Copy and ensure the app directory is owned by the chrome user 
COPY --from=build /app/ /app
WORKDIR /app/out

# Make sure the log file exist
RUN touch crossword_scraper_error.log

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.Scraper.dll"]

# Use this to debug the docker image - keeps it open instead of restarting it
# It is smart to use this in combination with using the SDK version as the runtime image
#CMD tail -f /dev/null
