# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*
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
# Stage final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Chuyển sang root để cài đặt và phân quyền
USER root

# Cài đặt client postgres và bash
RUN apt-get update && apt-get install -y postgresql-client bash && rm -rf /var/lib/apt/lists/*

# Copy file từ stage publish
COPY --from=publish /app/publish .
# Copy folder Migrations
COPY Codebase/Migrations ./Migrations
# Copy entrypoint script
COPY entrypoint.sh /usr/local/bin/entrypoint.sh

# SỬA LỖI TẠI ĐÂY: Tách biệt các lệnh hoặc dùng && đúng cách
RUN sed -i 's/\r$//' /usr/local/bin/entrypoint.sh && \
    chmod +x /usr/local/bin/entrypoint.sh && \
    sed -i 's/\r$//' ./Migrations/migrate.sh && \
    chmod +x ./Migrations/migrate.sh

# Phân quyền cho user của .NET
RUN chown -R $APP_UID:$APP_UID /app

# Chuyển về user mặc định để bảo mật
USER $APP_UID

# Entrypoint thực hiện: Chạy migrate xong mới khởi động app
ENTRYPOINT ["/bin/bash", "-c", "./Migrations/migrate.sh && dotnet Codebase.dll"]