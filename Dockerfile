# -----------------------------
# Base runtime image
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# -----------------------------
# Build stage
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["backend.csproj", "./"]
RUN dotnet restore "./backend.csproj"

COPY . .
RUN dotnet build "./backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -----------------------------
# Publish stage
# -----------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./backend.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


FROM base AS final

USER root

RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

USER 64198

WORKDIR /app
COPY --from=publish /app/publish .

# Render uses 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "backend.dll"]