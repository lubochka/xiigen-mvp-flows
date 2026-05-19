FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Xiigen.Flows.slnx
RUN dotnet publish flows/FLOW-04-event-attendance/server/Xiigen.Flow.EventAttendance.Api/Xiigen.Flow.EventAttendance.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Xiigen.Flow.EventAttendance.Api.dll"]

