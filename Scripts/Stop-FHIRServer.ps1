# Stop FHIR Server (HAPI)

Write-Host "=== Stopping FHIR Server ===" -ForegroundColor Cyan
Write-Host ""

$setupPath = Join-Path $PSScriptRoot "..\Phase1\Setup"

if (-not (Test-Path $setupPath)) {
    Write-Host "Error: Setup directory not found at $setupPath" -ForegroundColor Red
    exit 1
}

Push-Location $setupPath

try {
    Write-Host "Stopping Docker container..." -ForegroundColor Yellow
    docker-compose down
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "FHIR Server stopped successfully" -ForegroundColor Green
    }
    else {
        Write-Host "Failed to stop server" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}
