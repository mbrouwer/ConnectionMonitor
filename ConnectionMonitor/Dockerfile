# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

RUN apt-get update && apt-get install -y --no-install-recommends \
    nmap \
    inetutils-ping \
    jc \
    inotify-tools \
    && rm -rf /var/lib/apt/lists/*

#USER app
WORKDIR /app

COPY ["ConnectionMonitor/config/connectionTests.json", "./config/"]


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ConnectionMonitor/ConnectionMonitor.csproj", "ConnectionMonitor/"]
RUN dotnet restore "./ConnectionMonitor/ConnectionMonitor.csproj"
COPY . .
WORKDIR "/src/ConnectionMonitor"
RUN dotnet build "./ConnectionMonitor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConnectionMonitor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConnectionMonitor.dll"]