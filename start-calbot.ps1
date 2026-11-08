# start-calbot.ps1
# Launches Tailscale, Docker, Postgres, CalBot app and enables Tailscale Funnel.

# Убиваем всё, что висит на 7001
$stale = netstat -ano | Select-String ":7001" | Select-String "LISTENING"
foreach ($line in $stale) {
    $parts = ($line -split '\s+') | Where-Object { $_ -ne '' }
    $pid7001 = $parts[-1]
    if ($pid7001 -match '^\d+$') {
        Stop-Process -Id ([int]$pid7001) -Force -ErrorAction SilentlyContinue
    }
}

$ErrorActionPreference = "Stop"
$root = "C:\Users\Andrey\GitHub\CalBot"
$dockerExe = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
$publicUrl = "https://tiropito.tail88552b.ts.net/"

# ---------- Tailscale ----------
Write-Host "==> Checking Tailscale..." -ForegroundColor Cyan

$prevPref = $ErrorActionPreference
$ErrorActionPreference = "Continue"

$ts = Get-Service -Name Tailscale -ErrorAction SilentlyContinue
if ($null -eq $ts) {
    Write-Host "Tailscale service not found. Is Tailscale installed?" -ForegroundColor Red
    $ErrorActionPreference = $prevPref
    pause
    exit 1
}

if ($ts.Status -ne "Running") {
    Write-Host "Starting Tailscale service..." -ForegroundColor Yellow
    Start-Service -Name Tailscale
    Start-Sleep -Seconds 3
    Write-Host "Tailscale service started." -ForegroundColor Green
} else {
    Write-Host "Tailscale already running." -ForegroundColor DarkGray
}

# Запускаем tray-приложение (нужно для работы CLI-команд).
if (-not (Get-Process -Name "tailscale-ipn" -ErrorAction SilentlyContinue)) {
    $ipn = "C:\Program Files\Tailscale\tailscale-ipn.exe"
    if (Test-Path $ipn) {
        Start-Process -FilePath $ipn -WindowStyle Hidden -RedirectStandardOutput "$env:TEMP\tailscale-ipn.log" -RedirectStandardError "$env:TEMP\tailscale-ipn.err"
        Start-Sleep -Seconds 2
    }
}

$ErrorActionPreference = $prevPref

# ---------- Docker ----------
Write-Host "==> Checking Docker..." -ForegroundColor Cyan

function Wait-DockerReady {
    param([int]$TimeoutSec = 180)

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $prevPref = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5
        $null = & docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            $ErrorActionPreference = $prevPref
            return $true
        }
        Write-Host "  ... still waiting for Docker engine" -ForegroundColor DarkGray
    }

    $ErrorActionPreference = $prevPref
    return $false
}

$dockerRunning = Get-Process -Name "com.docker.backend" -ErrorAction SilentlyContinue
if (-not $dockerRunning) {
    Write-Host "Docker Desktop is not running, starting it..." -ForegroundColor Yellow
    if (Test-Path $dockerExe) {
        Start-Process $dockerExe
        Write-Host "Waiting for Docker Desktop to fully boot (up to 3 minutes)..." -ForegroundColor Yellow
        Write-Host "Do NOT run docker commands in other windows - it can freeze the engine." -ForegroundColor Yellow
    } else {
        Write-Host "Docker Desktop.exe not found at $dockerExe" -ForegroundColor Red
        pause
        exit 1
    }
}

if (-not (Wait-DockerReady -TimeoutSec 180)) {
    Write-Host ""
    Write-Host "Docker did not become ready within 3 minutes." -ForegroundColor Red
    Write-Host "Try these steps:" -ForegroundColor Yellow
    Write-Host "  1. Quit Docker Desktop from tray" -ForegroundColor Yellow
    Write-Host "  2. Run: wsl --shutdown" -ForegroundColor Yellow
    Write-Host "  3. Start Docker Desktop from the Start menu and wait 2 minutes" -ForegroundColor Yellow
    Write-Host "  4. Run this script again" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Docker engine is ready." -ForegroundColor Green

# ---------- Postgres ----------
Write-Host "==> Starting Postgres..." -ForegroundColor Cyan
Set-Location $root
docker compose up -d

Write-Host "==> Waiting for DB to become healthy..." -ForegroundColor Cyan
$prevPref = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$tries = 0
do {
    Start-Sleep -Seconds 3
    $status = (& docker compose ps --format "{{.Status}}" 2>&1 | Out-String)
    $tries++
    if ($tries -gt 40) {
        $ErrorActionPreference = $prevPref
        Write-Host "DB is not healthy after 2 minutes." -ForegroundColor Red
        docker compose logs db --tail 30
        pause
        exit 1
    }
} while ($status -notmatch "healthy")
$ErrorActionPreference = $prevPref

Write-Host "DB is healthy." -ForegroundColor Green

# ---------- CalBot app ----------
Write-Host "==> Starting CalBot app..." -ForegroundColor Cyan

# 1. Запускаем приложение в отдельном видимом окне
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\CalBot.Web'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run"

# 2. Запускаем скрытый фоновый наблюдатель, который ждёт закрытия CalBot.Web.exe
$watcherScript = @"
`$ErrorActionPreference = 'SilentlyContinue'
while (`$true) {
    Start-Sleep -Seconds 3
    `$proc = Get-Process -Name 'CalBot.Web' -ErrorAction SilentlyContinue
    if (-not `$proc) {
        # Приложение закрылось — запускаем cleanup скрыто
        Start-Sleep -Seconds 1
        Start-Process powershell -WindowStyle Hidden -ArgumentList '-ExecutionPolicy Bypass -File `"$root\stop-calbot.ps1`"'
        break
    }
}
"@

$watcherPath = "$env:TEMP\calbot-watcher.ps1"
Set-Content -Path $watcherPath -Value $watcherScript -Encoding UTF8

Start-Process powershell -WindowStyle Hidden -ArgumentList "-ExecutionPolicy Bypass -File `"$watcherPath`""

Write-Host "==> Waiting for app on port 7001..." -ForegroundColor Cyan
$up = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    try {
        $r = Invoke-WebRequest "https://localhost:7001" -SkipCertificateCheck -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
        $up = $true
        break
    } catch {
        if ($_.Exception.Response -ne $null) { $up = $true; break }
    }
}

if ($up) {
    Write-Host "App is listening on https://localhost:7001" -ForegroundColor Green
} else {
    Write-Host "App did not respond in 40 sec. Check the dotnet run window." -ForegroundColor Yellow
}

# ---------- Tailscale Funnel ----------
Write-Host "==> Enabling Tailscale Funnel..." -ForegroundColor Cyan
$prevPref = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$null = & tailscale funnel --bg --https=443 https+insecure://127.0.0.1:7001 2>&1
$ErrorActionPreference = $prevPref

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host " PUBLIC URL: $publicUrl" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Done." -ForegroundColor Green
Write-Host "To stop everything, use the 'CalBot Stop' shortcut." -ForegroundColor Gray