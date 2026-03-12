$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")
$runtimeDir = Join-Path $rootDir ".runtime"

$services = @(
    @{ Name = "Backend Python"; Url = "http://localhost:5000/api/health"; PidFile = (Join-Path $runtimeDir "backend-python.pid") },
    @{ Name = "Frontend React"; Url = "http://localhost:5173"; PidFile = (Join-Path $runtimeDir "frontend.pid") }
)

foreach ($service in $services) {
    $pidText = "(sem PID)"
    if (Test-Path $service.PidFile) {
        $pidText = (Get-Content $service.PidFile -Raw).Trim()
    }

    $status = "OFFLINE"
    try {
        $resp = Invoke-WebRequest -Uri $service.Url -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
            $status = "ONLINE"
        }
    } catch {}

    Write-Host ("{0}: {1} - PID: {2} - {3}" -f $service.Name, $status, $pidText, $service.Url)
}
