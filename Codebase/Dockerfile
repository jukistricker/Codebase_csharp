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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
# Cài đặt postgresql-client để có lệnh psql và pg_isready
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

# Chuyển sang root để setup script
USER root

# Copy toàn bộ folder Migrations vào /app/Migrations
COPY Codebase/Migrations ./Migrations

# 1. Chuyển đổi định dạng file từ Windows sang Linux (QUAN TRỌNG)
# 2. Cấp quyền thực thi cho script
RUN sed -i 's/\r$//' ./Migrations/migrate.sh && \
    chmod +x ./Migrations/migrate.sh

# Chuyển lại user của .NET để đảm bảo an toàn (Best Practice)
USER $APP_UID

# Entrypoint thực hiện: Chạy migrate xong mới chạy App
ENTRYPOINT ["/bin/bash", "-c", "./Migrations/migrate.sh && dotnet Codebase.dll"]