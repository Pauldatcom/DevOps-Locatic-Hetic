# Post-deploy checks (Windows).
param(
    [string]$NsApp = "locatic-staging",
    [string]$NsMon = "monitoring"
)

$ErrorActionPreference = "Stop"
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path", "User")

Write-Host "==> Resources in $NsApp"
kubectl get all,pvc -n $NsApp
Write-Host "==> Resources in $NsMon"
kubectl get all,pvc -n $NsMon

Write-Host "==> Health + metrics via Nginx"
$pf = Start-Process -FilePath kubectl -ArgumentList @(
    "port-forward", "-n", $NsApp, "svc/locatic-nginx", "8888:80"
) -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3
try {
    $health = (Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8888/health").Content.Trim()
    Write-Host "HEALTH: $health"
    if ($health -ne "Healthy") { throw "Unexpected health payload" }

    $metrics = Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8888/metrics"
    if ($metrics.StatusCode -ne 200) { throw "metrics status $($metrics.StatusCode)" }
    Write-Host "METRICS: OK (first lines)"
    $metrics.Content.Split("`n") | Select-Object -First 5 | ForEach-Object { Write-Host $_ }
}
finally {
    Stop-Process -Id $pf.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "==> Prometheus targets API"
$pf2 = Start-Process -FilePath kubectl -ArgumentList @(
    "port-forward", "-n", $NsMon, "svc/prometheus", "9090:9090"
) -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3
try {
    $targets = Invoke-RestMethod "http://127.0.0.1:9090/api/v1/targets"
    $active = $targets.data.activeTargets
    foreach ($t in $active) {
        Write-Host ("  {0,-16} {1,-10} {2}" -f $t.labels.job, $t.health, $t.labels.instance)
    }
    $appUp = $active | Where-Object { $_.labels.job -eq "locatic-app" -and $_.health -eq "up" }
    if (-not $appUp) {
        Write-Warning "locatic-app target not UP yet (wait ~30s and re-run)."
    } else {
        Write-Host "locatic-app scrape: UP"
    }
}
finally {
    Stop-Process -Id $pf2.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "==> OK"
Write-Host "Grafana: kubectl port-forward -n $NsMon svc/grafana 3000:3000  (admin / devops-training-local)"
