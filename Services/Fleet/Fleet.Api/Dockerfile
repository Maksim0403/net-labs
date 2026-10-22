# Контекст збірки — корінь репозиторію:
#   docker build -f Services/Fleet/Fleet.Api/Dockerfile -t fleet-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Services/Fleet/Fleet.Domain/Fleet.Domain.csproj Services/Fleet/Fleet.Domain/
COPY Services/Fleet/Fleet.Dal/Fleet.Dal.csproj Services/Fleet/Fleet.Dal/
COPY Services/Fleet/Fleet.Bll/Fleet.Bll.csproj Services/Fleet/Fleet.Bll/
COPY Services/Fleet/Fleet.Api/Fleet.Api.csproj Services/Fleet/Fleet.Api/
RUN dotnet restore Services/Fleet/Fleet.Api/Fleet.Api.csproj
COPY Services/Fleet/ Services/Fleet/
RUN dotnet publish Services/Fleet/Fleet.Api/Fleet.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fleet.Api.dll"]
