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
    throw "Target already exists: $target"
}

robocopy $source $target /E /XD .git .github node_modules dist .angular bin obj TestResults /XF *.log | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "robocopy failed with $LASTEXITCODE"
}

$removePaths = @(
    'ci',
    'flows\FLOW-00-bundle-activation',
    'flows\FLOW-01-user-registration',
    'flows\FLOW-02-profile-enrichment',
    'flows\FLOW-03-event-management',
    'flows\FLOW-04-event-attendance',
    'flows\FLOW-05-completion-gamification',
    'flows\FLOW-06-user-groups-communities',
    'flows\FLOW-07-friend-request-social-feed',
    'flows\FLOW-08-marketplace',
    'artifacts\nuget\FLOW-00',
    'artifacts\nuget\FLOW-01',
    'artifacts\nuget\FLOW-02',
    'artifacts\nuget\FLOW-03',
    'artifacts\nuget\FLOW-04',
    'artifacts\nuget\FLOW-05',
    'artifacts\nuget\FLOW-06',
    'artifacts\nuget\FLOW-07',
    'artifacts\nuget\FLOW-08',
    'docs\e2e-snapshots\bundle-activation',
    'docs\e2e-snapshots\user-registration',
    'docs\e2e-snapshots\profile-enrichment',
    'docs\e2e-snapshots\event-management',
    'docs\e2e-snapshots\event-attendance',
    'docs\e2e-snapshots\completion-gamification',
    'docs\e2e-snapshots\user-groups-communities',
    'docs\e2e-snapshots\friend-request-social-feed',
    'docs\e2e-snapshots\marketplace',
    'docs\translation-evidence\FLOW-00',
    'docs\translation-evidence\FLOW-01',
    'docs\translation-evidence\FLOW-02',
    'docs\translation-evidence\FLOW-03',
    'docs\translation-evidence\FLOW-04',
    'docs\translation-evidence\FLOW-05',
    'docs\translation-evidence\FLOW-06',
    'docs\translation-evidence\FLOW-07',
    'docs\translation-evidence\FLOW-08',
    'docs\sessions\FLOW-00',
    'docs\sessions\FLOW-01',
    'docs\sessions\FLOW-02',
    'docs\sessions\FLOW-03',
    'docs\sessions\FLOW-04',
    'docs\sessions\FLOW-05',
    'docs\sessions\FLOW-06',
    'docs\sessions\FLOW-07',
    'docs\sessions\FLOW-08'
)

foreach ($relative in $removePaths) {
    $path = Join-Path $target $relative
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}

$solution = @"
<Solution>
  <Folder Name="/flows/" />
  <Folder Name="/flows/FLOW-09-transactional-event-participation/" />
  <Folder Name="/flows/FLOW-09-transactional-event-participation/server/">
    <Project Path="flows/FLOW-09-transactional-event-participation/server/Xiigen.Flow.TransactionalEventParticipation.Api/Xiigen.Flow.TransactionalEventParticipation.Api.csproj" />
    <Project Path="flows/FLOW-09-transactional-event-participation/server/Xiigen.Flow.TransactionalEventParticipation.Application/Xiigen.Flow.TransactionalEventParticipation.Application.csproj" />
    <Project Path="flows/FLOW-09-transactional-event-participation/server/Xiigen.Flow.TransactionalEventParticipation.Infrastructure/Xiigen.Flow.TransactionalEventParticipation.Infrastructure.csproj" />
    <Project Path="flows/FLOW-09-transactional-event-participation/server/Xiigen.Flow.TransactionalEventParticipation.Tests/Xiigen.Flow.TransactionalEventParticipation.Tests.csproj" />
  </Folder>
</Solution>
"@
$solution | Set-Content -Encoding UTF8 (Join-Path $target 'Xiigen.Flows.slnx')

$tenantConfig = [pscustomobject]@{
    tenantId = $TenantId
    moduleName = $ModuleName
    sourceTenantId = $SourceTenantId
    cascadePoint = $CascadePoint
    flowId = 'FLOW-09'
    flowSlug = 'transactional-event-participation'
    version = $Version
    mode = $Mode
}
$tenantConfig | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $target 'tenant.config.json')

$manifestPath = Join-Path $target 'flows\FLOW-09-transactional-event-participation\flow.manifest.json'
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

$appPath = Join-Path $target 'flows\FLOW-09-transactional-event-participation\client\angular\src\app\app.ts'
$nodeEdit = @"
const fs = require('node:fs');
const file = process.argv[2];
const display = process.argv[3];
const original = fs.readFileSync(file, 'utf8');
const next = original.replace("readonly moduleName = signal('Event Ticketing');", "readonly moduleName = signal('" + display.replace(/'/g, "\\'") + "');");
fs.writeFileSync(file, next, 'utf8');
"@
$nodeEdit | node - $appPath $ModuleDisplay
if ($LASTEXITCODE -ne 0) {
    throw "node app.ts edit failed with exit code $LASTEXITCODE"
}

foreach ($relative in @(
    'flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\Xiigen.Flow.TransactionalEventParticipation.Application.csproj',
    'flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Infrastructure\Xiigen.Flow.TransactionalEventParticipation.Infrastructure.csproj'
)) {
    $path = Join-Path $target $relative
    (Get-Content -Raw $path) -replace '<Version>0\.1\.1</Version>', "<Version>$Version</Version>" |
        Set-Content -Encoding UTF8 $path
}

$packagePath = Join-Path $target 'flows\FLOW-09-transactional-event-participation\client\angular\package.json'
$package = Get-Content -Raw $packagePath | ConvertFrom-Json
$package.name = "@xiigen/$RepoName-angular"
$package.version = $Version
$package | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $packagePath

# GitHub Actions are intentionally not generated for tenant repositories.

Set-Location $target
$env:DOTNET_ROOT = 'C:\Projects\.dotnet'
$env:PATH = "C:\Projects\.dotnet;$env:PATH"
Invoke-CheckedNative dotnet restore Xiigen.Flows.slnx
Invoke-CheckedNative dotnet build Xiigen.Flows.slnx -c Release --no-restore
Invoke-CheckedNative dotnet test Xiigen.Flows.slnx -c Release --no-build

Set-Location (Join-Path $target 'flows\FLOW-09-transactional-event-participation\client\angular')
Invoke-CheckedNative npm install
Invoke-CheckedNative npm test
Invoke-CheckedNative npm run build

$out = Join-Path (Get-Location) 'tenant-static.out.log'
$err = Join-Path (Get-Location) 'tenant-static.err.log'
if (Test-Path $out) { Remove-Item $out -Force }
if (Test-Path $err) { Remove-Item $err -Force }
$server = Start-Process -FilePath node -ArgumentList 'scripts/serve-flow09-dist.mjs' -WorkingDirectory (Get-Location) -RedirectStandardOutput $out -RedirectStandardError $err -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 3
    $env:XIIGEN_FLOW09_BASE_URL = 'http://127.0.0.1:4309'
    $env:XIIGEN_CASCADE_POINT = $CascadePoint
    $env:XIIGEN_FLOW_MODULE_NAME = $ModuleName
    Invoke-CheckedNative npm run capture:evidence
} finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}

Set-Location $target
Invoke-CheckedNative git init -b main
Invoke-CheckedNative git add .
Invoke-CheckedNative git commit -m "Add FLOW-09 $CascadePoint"
$priorErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
gh repo view "lubochka/$RepoName" *> $null
$remoteExists = $LASTEXITCODE -eq 0
$ErrorActionPreference = $priorErrorActionPreference
if ($remoteExists) {
    Write-Output "Remote exists: lubochka/$RepoName"
} else {
    Invoke-CheckedNative gh repo create "lubochka/$RepoName" --private --description "XIIGen FLOW-09 $CascadePoint tenant repository"
}
Invoke-CheckedNative gh api -X PUT "repos/lubochka/$RepoName/actions/permissions" "-F" "enabled=false"

Invoke-CheckedNative git remote add origin "https://github.com/lubochka/$RepoName.git"
Invoke-CheckedNative git push -u origin main
Write-Output "TENANT_REPO_READY $RepoName $CascadePoint"
