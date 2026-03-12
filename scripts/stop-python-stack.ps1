$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")
$runtimeDir = Join-Path $rootDir ".runtime"

$pidFiles = @(
    (Join-Path $runtimeDir "backend-python.pid")
    (Join-Path $runtimeDir "frontend.pid")
)

foreach ($pidFile in $pidFiles) {
    if (-not (Test-Path $pidFile)) { continue }

    $pidRaw = Get-Content $pidFile -Raw
    $pidValue = 0
    if ([int]::TryParse($pidRaw.Trim(), [ref]$pidValue)) {
        $proc = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
        if ($proc) {
            Stop-Process -Id $pidValue -Force
            Write-Host "Processo $pidValue finalizado."
        }
    }

    Remove-Item $pidFile -Force
}

Write-Host "Stack parada."
