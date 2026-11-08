# stop-calbot.ps1
# Stops CalBot app, its PowerShell window, Tailscale Funnel, Tailscale service and Docker containers.

$ErrorActionPreference = "Continue"
$root = "C:\Users\Andrey\GitHub\CalBot"

# ---- Проверка прав администратора ----
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This script requires administrator rights." -ForegroundColor Red
    Write-Host "Right-click the shortcut and choose 'Run as administrator'." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    exit 1
}

Write-Host ""
Write-Host "Stopping CalBot..." -ForegroundColor Cyan
Write-Host ""

# ---- 1. Останавливаем CalBot (по порту 7001) ----
Write-Host "==> Stopping CalBot app..." -ForegroundColor Cyan

$killed = 0

# Ищем процесс, слушающий порт 7001, и убиваем его.
$conns = Get-NetTCPConnection -LocalPort 7001 -State Listen -ErrorAction SilentlyContinue
if ($conns) {
    $pids = $conns | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $pids) {
        try {
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "  Killing $($proc.ProcessName) (PID $procId)" -ForegroundColor DarkGray
                Stop-Process -Id $procId -Force
                $killed++
            }
        } catch { }
    }
}

# Дополнительно — ищем процессы dotnet, запущенные с "CalBot" в командной строке.
$cimProcs = Get-CimInstance Win32_Process -Filter "name='dotnet.exe'" -ErrorAction SilentlyContinue
foreach ($cp in $cimProcs) {
    if ($cp.CommandLine -and $cp.CommandLine -like "*CalBot*") {
        try {
            Write-Host "  Killing dotnet (PID $($cp.ProcessId))" -ForegroundColor DarkGray
            Stop-Process -Id $cp.ProcessId -Force
            $killed++
        } catch { }
    }
}

# И просто процесс CalBot.Web.exe, если он есть.
$webProcs = Get-Process -Name "CalBot.Web" -ErrorAction SilentlyContinue
foreach ($wp in $webProcs) {
    try {
        Write-Host "  Killing CalBot.Web (PID $($wp.Id))" -ForegroundColor DarkGray
        Stop-Process -Id $wp.Id -Force
        $killed++
    } catch { }
}

if ($killed -eq 0) {
    Write-Host "  No CalBot process found." -ForegroundColor DarkGray
} else {
    Write-Host "  Killed $killed process(es)." -ForegroundColor Green
}

Start-Sleep -Seconds 1

# ---- 2. Закрываем окна PowerShell, которые запустил start-calbot.ps1 ----
Write-Host "==> Closing CalBot PowerShell windows..." -ForegroundColor Cyan

$psProcs = Get-CimInstance Win32_Process -Filter "name='powershell.exe'" -ErrorAction SilentlyContinue
$psKilled = 0

foreach ($p in $psProcs) {
    $cmd = $p.CommandLine
    if (-not $cmd) { continue }

    # Ищем только окна, запущенные нашей командой:
    #   cd '...CalBot.Web'; $env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run
    $isOurs = ($cmd -like "*CalBot.Web*dotnet run*") -or
              ($cmd -like "*dotnet run*CalBot.Web*")

    if ($isOurs) {
        try {
            # Сначала гасим дочерние процессы (dotnet.exe, CalBot.Web.exe),
            # чтобы они не остались висеть после закрытия окна.
            $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.ProcessId)" -ErrorAction SilentlyContinue
            foreach ($child in $children) {
                Stop-Process -Id $child.ProcessId -Force -ErrorAction SilentlyContinue
            }
            Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
            $psKilled++
        } catch { }
    }
}

if ($psKilled -gt 0) {
    Write-Host "  Closed $psKilled CalBot PowerShell window(s)." -ForegroundColor Green
} else {
    Write-Host "  No CalBot PowerShell windows found." -ForegroundColor DarkGray
}

Start-Sleep -Seconds 1

# ---- 3. Отключаем Tailscale Funnel ----
Write-Host "==> Disabling Tailscale Funnel..." -ForegroundColor Cyan
& tailscale funnel --https=443 off 2>&1 | Out-Null
Write-Host "  Funnel disabled." -ForegroundColor Green

Start-Sleep -Seconds 1

# ---- 4. Останавливаем службу Tailscale ----
Write-Host "==> Stopping Tailscale service..." -ForegroundColor Cyan
$ts = Get-Service -Name Tailscale -ErrorAction SilentlyContinue
if ($ts -and $ts.Status -eq "Running") {
    Stop-Service -Name Tailscale -Force -ErrorAction SilentlyContinue
    Write-Host "  Tailscale stopped." -ForegroundColor Green
} else {
    Write-Host "  Tailscale is not running." -ForegroundColor DarkGray
}

$ipn = Get-Process -Name "tailscale-ipn" -ErrorAction SilentlyContinue
if ($ipn) {
    $ipn | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "  Tailscale tray closed." -ForegroundColor Green
}

Start-Sleep -Seconds 1

# ---- 5. Останавливаем Postgres в Docker ----
Write-Host "==> Stopping Docker containers..." -ForegroundColor Cyan
Set-Location $root
$dockerOutput = & docker compose down 2>&1
Write-Host "  $dockerOutput" -ForegroundColor DarkGray

Start-Sleep -Seconds 2

# ---- 6. Корректно завершаем WSL, потом гасим Docker Desktop ----
Write-Host "==> Shutting down WSL..." -ForegroundColor Cyan
$prevPref = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& wsl --shutdown 2>&1 | Out-Null
$ErrorActionPreference = $prevPref
Write-Host "  WSL shut down." -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host "==> Shutting down Docker Desktop..." -ForegroundColor Cyan
$dockerProcs = @("Docker Desktop", "com.docker.backend", "com.docker.build", "docker-agent", "docker-sandbox")
$dockerKilled = 0
foreach ($name in $dockerProcs) {
    $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($procs) {
        foreach ($p in $procs) {
            try {
                Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
                $dockerKilled++
            } catch { }
        }
    }
}

if ($dockerKilled -gt 0) {
    Write-Host "  Killed $dockerKilled Docker process(es)." -ForegroundColor Green
} else {
    Write-Host "  Docker Desktop was not running." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host " CalBot stopped." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

# Даём буферу вывода дойти до консоли, потом закрываем это окно.
Start-Sleep -Seconds 1

# Убиваем свой же процесс — окно PowerShell закроется.
Stop-Process -Id $PID -Force