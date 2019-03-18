FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
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
# FROM mcr.microsoft.com/dotnet/core/runtime:2.2
FROM alpine:3.9

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apk add --no-cache icu-libs

# Install .NET Core
ENV DOTNET_VERSION 2.2.3

RUN wget -O dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Runtime/$DOTNET_VERSION/dotnet-runtime-$DOTNET_VERSION-linux-musl-x64.tar.gz \
    && dotnet_sha512='b11e8731dd2e6b8738fb3a2762ed90de08df6661a8720ed76ef9429b99d763d0913ee100042a2995d72a13b50394a7e357397cecb23402c3104075efda04f62b' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz


RUN echo "http://dl-4.alpinelinux.org/alpine/v3.9/main" >> /etc/apk/repositories && \
    echo "http://dl-4.alpinelinux.org/alpine/v3.9/community" >> /etc/apk/repositories


RUN apk update && \
    apk add python py-pip curl unzip libexif udev chromium chromium-chromedriver xvfb && \
    pip install selenium && \
    pip install pyvirtualdisplay

# Install Chrome for Selenium
# RUN curl https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb -o /chrome.deb
# RUN dpkg -i /chrome.deb || apt-get install -yf
# RUN rm /chrome.deb

# Install chromedriver for Selenium
# RUN curl https://chromedriver.storage.googleapis.com/2.46/chromedriver_linux64.zip -o /usr/local/bin/chromedriver
# RUN chmod +x /usr/local/bin/chromedriver

# https://github.com/devpabloassis/seleniumdotnetcore/blob/master/Dockerfile

# https://github.com/justinribeiro/dockerfiles/blob/master/chrome-headless/Dockerfile

# Install deps + add Chrome Stable + purge all the things
# RUN apt-get update && apt-get install -y \
#     apt-transport-https \
#     ca-certificates \
#     curl \
#     gnupg \
#     --no-install-recommends \
#     && curl -sSL https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
#     && echo "deb https://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
#     && apt-get update && apt-get install -y \
#     google-chrome-stable \
#     fontconfig \
#     fonts-ipafont-gothic \
#     fonts-wqy-zenhei \
#     fonts-thai-tlwg \
#     fonts-kacst \
#     fonts-symbola \
#     fonts-noto \
#     ttf-freefont \
#     --no-install-recommends \
#     && apt-get purge --auto-remove -y curl gnupg \
#     && rm -rf /var/lib/apt/lists/*

# # Add Chrome as a user
# RUN groupadd -r chrome && useradd -r -g chrome -G audio,video chrome \
#     && mkdir -p /home/chrome && chown -R chrome:chrome /home/chrome \
#     && mkdir -p /opt/google/chrome && chown -R chrome:chrome /opt/google/chrome

# https://github.com/William-Yeh/docker-behave/blob/master/Dockerfile


WORKDIR /.
COPY --from=build-env /app/out .

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.Scraper.dll"]
