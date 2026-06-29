# FLOW-47 Design Conversion Intake

FLOW-47 is a minimal platform-facing scaffold for design conversion job intake.
It does not render, train, infer, or call real design providers.

## Boundary

- Submit job: `POST /flow-47/design-conversion/jobs`
- Get job status: `GET /flow-47/design-conversion/jobs/{jobId}`
- Current core dispatch is a scaffold fake through `IDesignConversionCoreClient`.
- Current persistence is a configurable file-backed durable job store through `IDesignConversionJobStore`.

## Intended Use

The platform can submit a design conversion request, receive a job id, and poll
status while the real core renderer/ML pipeline is integrated later.

## Not Implemented

- Real core rendering
- ML/training
- Figma/Canva/provider integration
- Cross-flow platform registration
- Central solution registration

## Local Verification

Use process-local SDK 10.0.300:

```powershell
$env:DOTNET_ROOT='C:\Users\lubao\.dotnet'
$env:PATH='C:\Users\lubao\.dotnet;' + $env:PATH
dotnet test flows\FLOW-47-design-conversion-intake\server\Xiigen.Flow.DesignConversionIntake.Tests\Xiigen.Flow.DesignConversionIntake.Tests.csproj
```
