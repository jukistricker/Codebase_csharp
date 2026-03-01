# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Codebase/Codebase.csproj", "Codebase/"]
RUN dotnet restore "./Codebase/Codebase.csproj"
COPY . .
WORKDIR "/src/Codebase"
RUN dotnet build "./Codebase.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Codebase.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy thư mục Migrations vào container để thực thi
# Đường dẫn trong container sẽ là /app/Migrations
COPY Codebase/Migrations ./Migrations

# Chuyển quyền thực thi cho script
USER root
RUN chmod +x ./Migrations/migrate.sh
USER $APP_UID

ENTRYPOINT ["/bin/bash", "-c", "./Migrations/migrate.sh && dotnet Codebase.dll"]