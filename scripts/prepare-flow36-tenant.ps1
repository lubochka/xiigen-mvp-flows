param(
    [Parameter(Mandatory = $true)][string]$RepoName,
    [Parameter(Mandatory = $true)][string]$TenantId,
    [Parameter(Mandatory = $true)][string]$ModuleName,
    [Parameter(Mandatory = $true)][string]$ModuleDisplay,
    [Parameter(Mandatory = $true)][string]$CascadePoint,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$SourceTenantId,
    [Parameter(Mandatory = $true)][string]$Mode
)

$ErrorActionPreference = 'Stop'
$source = 'C:\Projects\xiigen-flows-dot-net-angular'
$target = "C:\Projects\$RepoName"

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE"
    }
}

if (Test-Path $target) {
    $resolved = (Resolve-Path $target).Path
    if (-not $resolved.StartsWith('C:\Projects\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove non-project target: $resolved"
    }
    $stale = "$target-stale-$(Get-Date -Format 'yyyyMMddHHmmss')"
    Move-Item -LiteralPath $resolved -Destination $stale
    Write-Output "Moved existing tenant workspace to $stale"
}

robocopy $source $target /E /XD .git .github node_modules dist .angular bin obj TestResults /XF *.log | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "robocopy failed with $LASTEXITCODE"
}

foreach ($path in @('ci', '.github', 'docs\translation-evidence', 'docs\sessions')) {
    $candidate = Join-Path $target $path
    if (Test-Path $candidate) {
        Remove-Item -LiteralPath $candidate -Recurse -Force
    }
}

Get-ChildItem -LiteralPath $target -File |
    Where-Object { $_.Name -match '^[0-9]+$' } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

$flowsRoot = Join-Path $target 'flows'
Get-ChildItem -LiteralPath $flowsRoot -Directory |
    Where-Object { $_.Name -ne 'FLOW-36-feature-registry' } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

$artifactRoot = Join-Path $target 'artifacts\nuget'
if (Test-Path $artifactRoot) {
    Get-ChildItem -LiteralPath $artifactRoot -Directory |
        Where-Object { $_.Name -ne 'FLOW-36' } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}

$snapshotsRoot = Join-Path $target 'docs\e2e-snapshots'
if (Test-Path $snapshotsRoot) {
    Get-ChildItem -LiteralPath $snapshotsRoot -Directory |
        Where-Object { $_.Name -ne 'feature-registry' } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}

$solution = @"
<Solution>
  <Folder Name="/flows/" />
  <Folder Name="/flows/FLOW-36-feature-registry/" />
  <Folder Name="/flows/FLOW-36-feature-registry/server/">
    <Project Path="flows/FLOW-36-feature-registry/server/Xiigen.Flow.FeatureRegistry.Api/Xiigen.Flow.FeatureRegistry.Api.csproj" />
    <Project Path="flows/FLOW-36-feature-registry/server/Xiigen.Flow.FeatureRegistry.Application/Xiigen.Flow.FeatureRegistry.Application.csproj" />
    <Project Path="flows/FLOW-36-feature-registry/server/Xiigen.Flow.FeatureRegistry.Infrastructure/Xiigen.Flow.FeatureRegistry.Infrastructure.csproj" />
    <Project Path="flows/FLOW-36-feature-registry/server/Xiigen.Flow.FeatureRegistry.Tests/Xiigen.Flow.FeatureRegistry.Tests.csproj" />
  </Folder>
</Solution>
"@
$solution | Set-Content -Encoding UTF8 (Join-Path $target 'Xiigen.Flows.slnx')

$tenantConfig = [pscustomobject]@{
    tenantId = $TenantId
    moduleName = $ModuleName
    sourceTenantId = $SourceTenantId
    cascadePoint = $CascadePoint
    flowId = 'FLOW-36'
    flowSlug = 'feature-registry'
    version = $Version
    mode = $Mode
    actionsDisabled = $true
}
$tenantConfig | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $target 'tenant.config.json')

$manifestPath = Join-Path $target 'flows\FLOW-36-feature-registry\flow.manifest.json'
$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
$manifest.adaptation.defaultModuleName = $ModuleName
$manifest.certification.tierDComplete = $false
$manifest | Add-Member -NotePropertyName tenantCascade -NotePropertyValue ([pscustomobject]@{
    tenantId = $TenantId
    sourceTenantId = $SourceTenantId
    cascadePoint = $CascadePoint
    moduleName = $ModuleName
    mode = $Mode
}) -Force
$manifest | ConvertTo-Json -Depth 20 | Set-Content -Encoding UTF8 $manifestPath

$appPath = Join-Path $target 'flows\FLOW-36-feature-registry\client\angular\src\app\app.ts'
$nodeEdit = @"
const fs = require('node:fs');
const file = process.argv[2];
const display = process.argv[3];
const original = fs.readFileSync(file, 'utf8');
const next = original.replace("readonly moduleName = signal('Feature Registry');", "readonly moduleName = signal('" + display.replace(/'/g, "\\'") + "');");
fs.writeFileSync(file, next, 'utf8');
"@
$nodeEdit | node - $appPath $ModuleDisplay
if ($LASTEXITCODE -ne 0) {
    throw "node app.ts edit failed with exit code $LASTEXITCODE"
}

foreach ($relative in @(
    'flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Application\Xiigen.Flow.FeatureRegistry.Application.csproj',
    'flows\FLOW-36-feature-registry\server\Xiigen.Flow.FeatureRegistry.Infrastructure\Xiigen.Flow.FeatureRegistry.Infrastructure.csproj'
)) {
    $path = Join-Path $target $relative
    (Get-Content -Raw $path) -replace '<Version>0\.1\.1</Version>', "<Version>$Version</Version>" |
        Set-Content -Encoding UTF8 $path
}

$packagePath = Join-Path $target 'flows\FLOW-36-feature-registry\client\angular\package.json'
$package = Get-Content -Raw $packagePath | ConvertFrom-Json
$package.name = "@xiigen/$RepoName-angular"
$package.version = $Version
$package | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $packagePath

Set-Location $target
$env:DOTNET_ROOT = 'C:\Projects\.dotnet'
$env:PATH = "C:\Projects\.dotnet;$env:PATH"
Invoke-CheckedNative dotnet restore Xiigen.Flows.slnx
Invoke-CheckedNative dotnet build Xiigen.Flows.slnx -c Release --no-restore
Invoke-CheckedNative dotnet test Xiigen.Flows.slnx -c Release --no-build

Set-Location (Join-Path $target 'flows\FLOW-36-feature-registry\client\angular')
Invoke-CheckedNative npm install
Invoke-CheckedNative npm test
Invoke-CheckedNative npm run build

$out = Join-Path (Get-Location) 'tenant-static.out.log'
$err = Join-Path (Get-Location) 'tenant-static.err.log'
if (Test-Path $out) { Remove-Item $out -Force }
if (Test-Path $err) { Remove-Item $err -Force }
$server = Start-Process -FilePath node -ArgumentList 'scripts/serve-flow36-dist.mjs' -WorkingDirectory (Get-Location) -RedirectStandardOutput $out -RedirectStandardError $err -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 3
    $env:FLOW36_BASE_URL = 'http://127.0.0.1:4336'
    $env:FLOW36_MODULE_NAME = $ModuleDisplay
    $env:FLOW36_EVIDENCE_DIR = Join-Path $target "docs\e2e-snapshots\feature-registry\dotnet-angular\$CascadePoint"
    Invoke-CheckedNative npm run capture:evidence
} finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}

Set-Location $target
Invoke-CheckedNative git init -b main
Invoke-CheckedNative git add .
Invoke-CheckedNative git commit -m "Add FLOW-36 $CascadePoint"
$priorErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
gh repo view "lubochka/$RepoName" *> $null
$remoteExists = $LASTEXITCODE -eq 0
$ErrorActionPreference = $priorErrorActionPreference
if ($remoteExists) {
    Write-Output "Remote exists: lubochka/$RepoName"
} else {
    Invoke-CheckedNative gh repo create "lubochka/$RepoName" --private --description "XIIGen FLOW-36 $CascadePoint tenant repository"
}
Invoke-CheckedNative gh api -X PUT "repos/lubochka/$RepoName/actions/permissions" "-F" "enabled=false"

Invoke-CheckedNative git remote add origin "https://github.com/lubochka/$RepoName.git"
Invoke-CheckedNative git push -u origin main --force
Set-Location $source
Write-Output "TENANT_REPO_READY $RepoName $CascadePoint"




