[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSCommandPath
$setupScript = Join-Path $repoRoot "setup-functions-tools.ps1"
$frontendProject = Join-Path $repoRoot "PigeonsTracker\PigeonsTracker.csproj"
$apiDir = Join-Path $repoRoot "PigeonsTrackerApi"

if (-not (Test-Path $frontendProject)) {
    throw "Frontend project not found: $frontendProject"
}

if (-not (Test-Path $apiDir)) {
    throw "API directory not found: $apiDir"
}

if (-not (Test-Path $setupScript)) {
    throw "Setup script not found: $setupScript"
}

function Ensure-FunctionsTools {
    $funcCommand = Get-Command func -ErrorAction SilentlyContinue
    if ($null -ne $funcCommand) {
        return
    }

    Write-Host "Azure Functions Core Tools not found. Running setup..." -ForegroundColor Yellow
    & $setupScript
    
    $funcCommand = Get-Command func -ErrorAction SilentlyContinue
    if ($null -eq $funcCommand) {
        throw "Failed to set up Azure Functions Core Tools. Run setup-functions-tools.ps1 manually."
    }
}

function Load-LocalSettings {
    $localSettingsPath = Join-Path $apiDir "local.settings.json"
    if (Test-Path $localSettingsPath) {
        Write-Host "Loading local settings..." -ForegroundColor Gray
        $settings = Get-Content $localSettingsPath -Raw | ConvertFrom-Json
        foreach ($key in $settings.Values.PSObject.Properties.Name) {
            $value = $settings.Values.$key
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
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
    Write-Host "`nStarting frontend with dotnet watch and API with func start..." -ForegroundColor Cyan

    $script:frontendJob = Start-Job `
        -Name "frontend-watch" `
        -ScriptBlock {
            param($projectPath, $rootPath)
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
            $OutputEncoding = [System.Text.Encoding]::UTF8
            Set-Location $rootPath
            & dotnet watch --project $projectPath run 2>&1 | ForEach-Object { "[frontend] $_" }
        } `
        -ArgumentList @($frontendProject, $repoRoot)

    $script:apiJob = Start-Job `
        -Name "api-func" `
        -ScriptBlock {
            param($apiPath)
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
            $OutputEncoding = [System.Text.Encoding]::UTF8
            Set-Location $apiPath
            & dotnet run 2>&1 | ForEach-Object { "[api] $_" }
        } `
        -ArgumentList @($apiDir)

    $script:reportedExitedJobs = @{}

    Write-Host "Frontend Job: $($script:frontendJob.Id) | API Job: $($script:apiJob.Id)" -ForegroundColor DarkGray
}

try {
    Ensure-FunctionsTools
    Load-LocalSettings
    Write-Host "Dev launcher ready." -ForegroundColor Green
    Write-Host "Press Ctrl+R to restart frontend and API. Press Ctrl+C to stop." -ForegroundColor Yellow

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
