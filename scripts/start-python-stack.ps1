$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")
$runtimeDir = Join-Path $rootDir ".runtime"
$backendDir = Join-Path $rootDir "backend-python"
$frontendDir = Join-Path $rootDir "frontend"

New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null

function Resolve-PythonPath {
    $venvPython = Join-Path $backendDir ".venv\Scripts\python.exe"
    if (Test-Path $venvPython) { return $venvPython }

    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Python\Python314\python.exe",
        "$env:LOCALAPPDATA\Programs\Python\Python313\python.exe",
        "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw "Python nao encontrado. Instale Python 3.12+ e tente novamente."
}

function Wait-Url {
    param(
        [Parameter(Mandatory = $true)] [string]$Url,
        [Parameter(Mandatory = $true)] [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
                return $true
            }
        } catch {
            Start-Sleep -Milliseconds 800
        }
    }

    return $false
}

$pythonExe = Resolve-PythonPath
$venvPython = Join-Path $backendDir ".venv\Scripts\python.exe"

if (-not (Test-Path $venvPython)) {
    & $pythonExe -m venv (Join-Path $backendDir ".venv")
}

& $venvPython -m pip install -r (Join-Path $backendDir "requirements.txt") | Out-Null

$sqliteFile = Join-Path $backendDir "todo.db"
$sqliteFileNormalized = $sqliteFile -replace "\\", "/"
$databaseUrl = "sqlite:///$sqliteFileNormalized"

Push-Location $backendDir
$env:DATABASE_URL = $databaseUrl
& $venvPython -m alembic -c alembic.ini upgrade head | Out-Null
Pop-Location

$frontendEnvFile = Join-Path $frontendDir ".env"
if (-not (Test-Path $frontendEnvFile)) {
    "VITE_API_URL=http://localhost:5000/api" | Set-Content $frontendEnvFile
}

Push-Location $frontendDir
npm.cmd install | Out-Null
Pop-Location

$backendOut = Join-Path $runtimeDir "backend-python.out.log"
$backendErr = Join-Path $runtimeDir "backend-python.err.log"
$frontendOut = Join-Path $runtimeDir "frontend.out.log"
$frontendErr = Join-Path $runtimeDir "frontend.err.log"

if (Test-Path $backendOut) { Remove-Item $backendOut -Force -ErrorAction SilentlyContinue }
if (Test-Path $backendErr) { Remove-Item $backendErr -Force -ErrorAction SilentlyContinue }
if (Test-Path $frontendOut) { Remove-Item $frontendOut -Force -ErrorAction SilentlyContinue }
if (Test-Path $frontendErr) { Remove-Item $frontendErr -Force -ErrorAction SilentlyContinue }

$env:DATABASE_URL = $databaseUrl
$env:PORT = "5000"
$env:FLASK_DEBUG = "0"
$env:CORS_ALLOWED_ORIGINS = "http://localhost:5173"

$backendProc = Start-Process -FilePath $venvPython -ArgumentList "run.py" -WorkingDirectory $backendDir -PassThru -WindowStyle Hidden -RedirectStandardOutput $backendOut -RedirectStandardError $backendErr
$frontendProc = Start-Process -FilePath "npm.cmd" -ArgumentList "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173", "--strictPort" -WorkingDirectory $frontendDir -PassThru -WindowStyle Hidden -RedirectStandardOutput $frontendOut -RedirectStandardError $frontendErr

$backendPidFile = Join-Path $runtimeDir "backend-python.pid"
$frontendPidFile = Join-Path $runtimeDir "frontend.pid"

$backendProc.Id | Set-Content $backendPidFile
$frontendProc.Id | Set-Content $frontendPidFile

$backendOk = Wait-Url -Url "http://localhost:5000/api/health" -TimeoutSeconds 30
$frontendOk = Wait-Url -Url "http://localhost:5173" -TimeoutSeconds 45

if (-not $backendOk -or -not $frontendOk) {
    Write-Host "Falha ao iniciar algum servico. Verifique logs em .runtime/."
    exit 1
}

Write-Host "Stack iniciada com sucesso."
Write-Host "Backend Python: http://localhost:5000/api/health"
Write-Host "Frontend React: http://localhost:5173"
Write-Host "Para parar: powershell -ExecutionPolicy Bypass -File .\scripts\stop-python-stack.ps1"
