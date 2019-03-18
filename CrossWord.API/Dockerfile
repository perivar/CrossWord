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
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
WORKDIR /.
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

# Change name of dll for your application 
ENTRYPOINT ["dotnet", "CrossWord.API.dll"]
