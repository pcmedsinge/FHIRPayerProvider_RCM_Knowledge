# Check FHIR Server Status

Write-Host "=== FHIR Server Status ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker container is running
$container = docker ps --filter "name=fhir-server-payer" --format "{{.Status}}"

if ($container) {
    Write-Host "Docker Container: Running" -ForegroundColor Green
    Write-Host "Status: $container" -ForegroundColor White
    Write-Host ""
    
    # Check if FHIR API is responding
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:8082/fhir/metadata" -Method Get -TimeoutSec 5
        Write-Host "FHIR API: Responding" -ForegroundColor Green
        Write-Host "FHIR Version: $($response.fhirVersion)" -ForegroundColor White
        Write-Host "Server Status: $($response.status)" -ForegroundColor White
        Write-Host ""
        
        # Get resource counts
        Write-Host "Data Summary:" -ForegroundColor Yellow
        $resources = @("Patient", "ExplanationOfBenefit", "Coverage", "Encounter")
        foreach ($resource in $resources) {
            try {
                $result = Invoke-RestMethod -Uri "http://localhost:8082/fhir/$resource`?_summary=count" -Method Get -TimeoutSec 5
                Write-Host "  $resource`: $($result.total)" -ForegroundColor White
            }
            catch {
                Write-Host "  $resource`: Error querying" -ForegroundColor Gray
            }
        }
        
        Write-Host ""
        Write-Host "Access Points:" -ForegroundColor Cyan
        Write-Host "  FHIR API: http://localhost:8082/fhir" -ForegroundColor White
        Write-Host "  Web UI: http://localhost:8082" -ForegroundColor White
    }
    catch {
        Write-Host "FHIR API: Not responding (still initializing or error)" -ForegroundColor Yellow
        Write-Host "Error: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "Docker Container: Not Running" -ForegroundColor Red
    Write-Host ""
    Write-Host "To start the server, run: .\Scripts\Start-FHIRServer.ps1" -ForegroundColor Yellow
}
