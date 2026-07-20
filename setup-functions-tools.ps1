[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$ToolsDir = "$env:LOCALAPPDATA\AzureFunctionTools"
$ZipPath = "$env:TEMP\func.zip"
$DownloadUrl = "https://github.com/Azure/azure-functions-core-tools/releases/download/4.12.1/Azure.Functions.Cli.win-x64.4.12.1.zip"

Write-Host "Setting up Azure Functions Core Tools..." -ForegroundColor Cyan

if (Get-Command func -ErrorAction SilentlyContinue) {
    Write-Host "func is already available in PATH" -ForegroundColor Green
    func --version
    exit 0
}

Write-Host "Downloading Azure Functions Core Tools..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -TimeoutSec 300 -ErrorAction Stop
    Write-Host "Download completed" -ForegroundColor Green
} catch {
    Write-Host "Download failed: $_" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ToolsDir)) {
    New-Item -ItemType Directory -Path $ToolsDir -Force | Out-Null
}

Write-Host "Extracting to $ToolsDir..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $ZipPath -DestinationPath $ToolsDir -Force -ErrorAction Stop
    Write-Host "Extraction completed" -ForegroundColor Green
} catch {
    Write-Host "Extraction failed: $_" -ForegroundColor Red
    exit 1
}

$FuncExe = Join-Path $ToolsDir "func.exe"
if (-not (Test-Path $FuncExe)) {
    Write-Host "ERROR: func.exe not found after extraction" -ForegroundColor Red
    exit 1
}

Write-Host "Verifying installation..." -ForegroundColor Yellow
& $FuncExe --version
if ($LASTEXITCODE -eq 0) {
    Write-Host "Adding to PATH..." -ForegroundColor Yellow
    $env:Path = "$ToolsDir;$env:Path"
    [Environment]::SetEnvironmentVariable("Path", "$ToolsDir;$env:Path", "User")
    Write-Host "Setup completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Verification failed" -ForegroundColor Red
    exit 1
}

Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
