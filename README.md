# XIIGen .NET/Angular Flows

This repository contains translated XIIGen flow implementations. FLOW-00 is implemented first as the bundle activation sentinel.

## FLOW-00

- .NET 10 API, application, infrastructure, and tests: `flows/FLOW-00-bundle-activation/server`
- Angular 21 standalone client: `flows/FLOW-00-bundle-activation/client/angular`
- Flow manifest, endpoint descriptors, auth policy registry, Docker, CI, QA, and visual evidence are included.

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File scripts\pre-commit-check.ps1
docker compose -f flows\FLOW-00-bundle-activation\docker\docker-compose.flow.yml build bundle-activation-api
```
