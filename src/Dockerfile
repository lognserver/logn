﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0.100-preview.5 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Logn/Logn.csproj", "Logn/"]
RUN dotnet restore "Logn/Logn.csproj"
COPY . .
WORKDIR "/src/Logn"
RUN dotnet build "./Logn.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logn.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logn.dll"]
