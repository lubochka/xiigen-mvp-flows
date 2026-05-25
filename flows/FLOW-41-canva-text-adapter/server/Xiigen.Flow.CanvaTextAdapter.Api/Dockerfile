FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish flows/FLOW-41-canva-text-adapter/server/Xiigen.Flow.CanvaTextAdapter.Api/Xiigen.Flow.CanvaTextAdapter.Api.csproj \
    -c Release \
    -o /app/publish \
    --locked-mode

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Xiigen.Flow.CanvaTextAdapter.Api.dll"]
