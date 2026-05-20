# Server Management Scripts

This folder contains scripts to manage the FHIR server and other infrastructure.

## Available Scripts

### Start-FHIRServer.ps1
Starts the HAPI FHIR server Docker container.

```powershell
.\Scripts\Start-FHIRServer.ps1
```

**What it does:**
- Starts the Docker container
- Waits for server initialization
- Verifies the server is responding
- Displays access URLs

---

### Stop-FHIRServer.ps1
Stops the FHIR server Docker container.

```powershell
.\Scripts\Stop-FHIRServer.ps1
```

**What it does:**
- Gracefully stops the Docker container
- Preserves data in volumes

---

### Check-FHIRServer.ps1
Checks the status of the FHIR server.

```powershell
.\Scripts\Check-FHIRServer.ps1
```

**What it does:**
- Checks if Docker container is running
- Verifies FHIR API is responding
- Shows resource counts (Patients, Claims, etc.)
- Displays access URLs

---

### Restart-FHIRServer.ps1
Restarts the FHIR server (stop + start).

```powershell
.\Scripts\Restart-FHIRServer.ps1
```

**What it does:**
- Stops the server
- Waits a few seconds
- Starts the server again

---

## Quick Reference

| Task | Command |
|------|---------|
| Start server | `.\Scripts\Start-FHIRServer.ps1` |
| Stop server | `.\Scripts\Stop-FHIRServer.ps1` |
| Check status | `.\Scripts\Check-FHIRServer.ps1` |
| Restart | `.\Scripts\Restart-FHIRServer.ps1` |

---

## FHIR Server Details

- **Base URL**: http://localhost:8082/fhir
- **Web UI**: http://localhost:8082
- **Container Name**: fhir-server-payer
- **Technology**: HAPI FHIR R4
- **Storage**: Docker volume (persists between restarts)

---

## Troubleshooting

### Server won't start
1. Check Docker Desktop is running
2. Check port 8082 is not in use: `netstat -ano | findstr :8082`
3. View logs: `docker logs fhir-server-payer`

### Server not responding
1. Wait 30-60 seconds after starting (initialization takes time)
2. Check logs: `docker logs fhir-server-payer`
3. Restart: `.\Scripts\Restart-FHIRServer.ps1`

### Data is gone after restart
Data should persist in Docker volumes. Check:
```powershell
docker volume ls | findstr hapi
```

To completely remove data and start fresh:
```powershell
cd Phase1\Setup
docker-compose down -v
```

---

## Future Scripts

Additional scripts can be added here for:
- Backup/restore data
- Load test data
- Export data
- Performance monitoring
- Log viewing
