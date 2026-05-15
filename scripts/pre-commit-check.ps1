$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
  $dotnet = 'dotnet'
}

Push-Location $root
try {
  & $dotnet restore 'Xiigen.Flows.slnx'
  & $dotnet build 'Xiigen.Flows.slnx' --configuration Release --no-restore
  & $dotnet test 'Xiigen.Flows.slnx' --configuration Release --no-build

  Push-Location 'flows/FLOW-00-bundle-activation/client/angular'
  try {
    npm ci
    npm run build
    npm test
    $server = Start-Process -FilePath 'node' -ArgumentList @('scripts/serve-flow00-dist.mjs') -PassThru -WindowStyle Hidden
    try {
      $ready = $false
      for ($i = 0; $i -lt 30; $i++) {
        try {
          Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:4300/' -TimeoutSec 2 | Out-Null
          $ready = $true
          break
        } catch {
          Start-Sleep -Seconds 1
        }
      }
      if (-not $ready) {
        throw 'FLOW-00 Angular static server did not become ready.'
      }
      node scripts/capture-flow00-evidence.mjs
    } finally {
      Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
    }
  } finally {
    Pop-Location
  }
} finally {
  Pop-Location
}
