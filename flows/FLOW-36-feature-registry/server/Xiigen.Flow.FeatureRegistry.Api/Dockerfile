FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish flows/FLOW-36-feature-registry/server/Xiigen.Flow.FeatureRegistry.Api/Xiigen.Flow.FeatureRegistry.Api.csproj \
    -c Release \
    -o /app/publish \
    --locked-mode

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Xiigen.Flow.FeatureRegistry.Api.dll"]

