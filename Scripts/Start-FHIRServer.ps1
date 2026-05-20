# Start FHIR Server (HAPI)

Write-Host "=== Starting FHIR Server ===" -ForegroundColor Cyan
Write-Host ""

$setupPath = Join-Path $PSScriptRoot "..\Phase1\Setup"

if (-not (Test-Path $setupPath)) {
    Write-Host "Error: Setup directory not found at $setupPath" -ForegroundColor Red
    exit 1
}

Push-Location $setupPath

try {
    Write-Host "Starting Docker container..." -ForegroundColor Yellow
    docker-compose up -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Server is starting..." -ForegroundColor Green
        Write-Host "Waiting for initialization (30 seconds)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        # Check if server is responding
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:8082/fhir/metadata" -Method Get -TimeoutSec 5
            Write-Host ""
            Write-Host "=== FHIR Server Ready! ===" -ForegroundColor Green
            Write-Host "FHIR Base URL: http://localhost:8082/fhir" -ForegroundColor White
            Write-Host "Web UI: http://localhost:8082" -ForegroundColor White
            Write-Host "FHIR Version: $($response.fhirVersion)" -ForegroundColor White
        }
        catch {
            Write-Host ""
            Write-Host "Server is starting but not ready yet." -ForegroundColor Yellow
            Write-Host "Check status with: .\Scripts\Check-FHIRServer.ps1" -ForegroundColor White
        }
    }
    else {
        Write-Host "Failed to start server" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}
