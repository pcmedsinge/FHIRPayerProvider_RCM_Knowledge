# Restart FHIR Server (HAPI)

Write-Host "=== Restarting FHIR Server ===" -ForegroundColor Cyan
Write-Host ""

$scriptPath = $PSScriptRoot

# Stop the server
Write-Host "Step 1: Stopping server..." -ForegroundColor Yellow
& "$scriptPath\Stop-FHIRServer.ps1"

Write-Host ""
Write-Host "Waiting 5 seconds..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Start the server
Write-Host ""
Write-Host "Step 2: Starting server..." -ForegroundColor Yellow
& "$scriptPath\Start-FHIRServer.ps1"
