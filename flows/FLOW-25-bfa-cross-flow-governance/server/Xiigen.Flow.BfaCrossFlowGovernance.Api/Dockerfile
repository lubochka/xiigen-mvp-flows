FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Xiigen.Flows.slnx
RUN dotnet publish flows/FLOW-25-bfa-cross-flow-governance/server/Xiigen.Flow.BfaCrossFlowGovernance.Api/Xiigen.Flow.BfaCrossFlowGovernance.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Xiigen.Flow.BfaCrossFlowGovernance.Api.dll"]

