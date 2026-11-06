FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Platform.ServiceDefaults/Platform.ServiceDefaults.csproj Platform.ServiceDefaults/
COPY Platform.Aggregator/Platform.Aggregator.csproj Platform.Aggregator/
RUN dotnet restore Platform.Aggregator/Platform.Aggregator.csproj
COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.Aggregator/ Platform.Aggregator/
RUN dotnet publish Platform.Aggregator/Platform.Aggregator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Platform.Aggregator.dll"]

