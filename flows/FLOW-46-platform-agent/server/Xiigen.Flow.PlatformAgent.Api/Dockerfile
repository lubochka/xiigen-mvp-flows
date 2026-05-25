FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish flows/FLOW-45-history-bootstrap/server/Xiigen.Flow.HistoryBootstrap.Api/Xiigen.Flow.HistoryBootstrap.Api.csproj \
    -c Release \
    -o /app/publish \
    --locked-mode

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Xiigen.Flow.HistoryBootstrap.Api.dll"]

