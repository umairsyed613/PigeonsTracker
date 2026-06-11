[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSCommandPath
$frontendProject = Join-Path $repoRoot "PigeonsTracker\PigeonsTracker.csproj"
$apiProject = Join-Path $repoRoot "PigeonsTrackerApi\PigeonsTrackerApi.csproj"

if (-not (Test-Path $frontendProject)) {
    throw "Frontend project not found: $frontendProject"
}

if (-not (Test-Path $apiProject)) {
    throw "API project not found: $apiProject"
}

function Ensure-FunctionsCoreTools {
    $funcCommand = Get-Command func -ErrorAction SilentlyContinue
    if ($null -ne $funcCommand) {
        return
    }

    Write-Host "Azure Functions Core Tools not found. Installing with npm..." -ForegroundColor Yellow
    & npm install -g azure-functions-core-tools@4 --unsafe-perm true
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install Azure Functions Core Tools. Install manually with: npm install -g azure-functions-core-tools@4 --unsafe-perm true"
    }

    $funcCommand = Get-Command func -ErrorAction SilentlyContinue
    if ($null -eq $funcCommand) {
        throw "Azure Functions Core Tools installation completed but 'func' is still unavailable. Restart your terminal and run start-dev.ps1 again."
    }
}

$script:frontendJob = $null
$script:apiJob = $null
$script:reportedExitedJobs = @{}

function Stop-Watchers {
    foreach ($job in @($script:frontendJob, $script:apiJob)) {
        if ($null -ne $job) {
            Stop-Job -Job $job -ErrorAction SilentlyContinue
            Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
        }
    }
}

function Start-Watchers {
    Write-Host "`nStarting frontend with dotnet watch and API with dotnet run..." -ForegroundColor Cyan

    $script:frontendJob = Start-Job `
        -Name "frontend-watch" `
        -ScriptBlock {
            param($projectPath, $rootPath)
            Set-Location $rootPath
            & dotnet watch --project $projectPath run 2>&1 | ForEach-Object { "[frontend] $_" }
        } `
        -ArgumentList @($frontendProject, $repoRoot)

    $script:apiJob = Start-Job `
        -Name "api-watch" `
        -ScriptBlock {
            param($projectPath, $rootPath)
            Set-Location (Split-Path -Parent $projectPath)
            & dotnet run --project $projectPath 2>&1 | ForEach-Object { "[api] $_" }
        } `
        -ArgumentList @($apiProject, $repoRoot)

    $script:reportedExitedJobs = @{}

    Write-Host "Frontend Job: $($script:frontendJob.Id) | API Job: $($script:apiJob.Id)" -ForegroundColor DarkGray
}

try {
    Ensure-FunctionsCoreTools
    Write-Host "Dev launcher ready." -ForegroundColor Green
    Write-Host "Press Ctrl+R to restart frontend watch and API run. Press Ctrl+C to stop." -ForegroundColor Yellow

    Start-Watchers

    while ($true) {
        foreach ($line in (Receive-Job -Job @($script:frontendJob, $script:apiJob) -ErrorAction SilentlyContinue)) {
            if ($line -is [string] -and $line.StartsWith("[frontend]")) {
                Write-Host $line -ForegroundColor Gray
            } elseif ($line -is [string] -and $line.StartsWith("[api]")) {
                Write-Host $line -ForegroundColor DarkCyan
            } else {
                Write-Host $line
            }
        }

        foreach ($job in @($script:frontendJob, $script:apiJob)) {
            if ($null -ne $job -and $job.State -in @("Failed", "Completed", "Stopped")) {
                if (-not $script:reportedExitedJobs.ContainsKey($job.Id)) {
                    Write-Host "Watcher '$($job.Name)' exited with state: $($job.State). Use Ctrl+R to restart." -ForegroundColor Red
                    $script:reportedExitedJobs[$job.Id] = $true
                }
            }
        }

        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true)
            $isCtrlR = ($key.Key -eq [ConsoleKey]::R) -and (($key.Modifiers -band [ConsoleModifiers]::Control) -ne 0)

            if ($isCtrlR) {
                Write-Host "`nCtrl+R detected. Restarting frontend and API..." -ForegroundColor Yellow
                Stop-Watchers
                Start-Sleep -Milliseconds 250
                Start-Watchers
            }
        }

        Start-Sleep -Milliseconds 150
    }
}
finally {
    Stop-Watchers
}
