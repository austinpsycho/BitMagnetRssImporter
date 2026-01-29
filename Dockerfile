# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first for restore caching
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASP.NET Core in containers should listen on 0.0.0.0
ENV ASPNETCORE_URLS=http://0.0.0.0:8085
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ConnectionStrings__Sqlite=Data Source=/data/app.db


COPY --from=build /app/publish ./

EXPOSE 8085
ENTRYPOINT ["dotnet", "BitMagnetRssImporter.dll"]