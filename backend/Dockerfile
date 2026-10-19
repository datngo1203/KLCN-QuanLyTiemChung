FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY backend/ backend/
RUN dotnet publish backend/src/TiemChung.Api/TiemChung.Api.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TiemChung.Api.dll"]
