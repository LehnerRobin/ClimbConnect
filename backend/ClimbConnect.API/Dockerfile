FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClimbConnect.API.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER root
RUN mkdir -p /app/Data /app/uploads && chmod -R 777 /app/Data /app/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "ClimbConnect.API.dll"]
