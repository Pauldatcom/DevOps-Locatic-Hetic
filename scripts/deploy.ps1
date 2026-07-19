# Deploy Locatic on Windows with Docker Desktop + minikube.
# Portable for any Windows machine (students / professor).
# Usage: .\scripts\deploy.ps1 [-EnvName dev] [-ClusterType minikube]
param(
    [ValidateSet("dev", "prod")]
    [string]$EnvName = "dev",
    [ValidateSet("minikube", "kind")]
    [string]$ClusterType = "minikube"
)

$ErrorActionPreference = "Stop"
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path", "User")

$Root = Split-Path -Parent $PSScriptRoot
$TfDir = Join-Path $Root "infra\terraform\environments\$EnvName"
$AnsibleDir = Join-Path $Root "infra\ansible"
# Tag unique a chaque run : evite le cache minikube qui garde une vieille :latest.
$ImageTag = "local-$(Get-Date -Format 'yyyyMMddHHmmss')"
$Image = "ghcr.io/pauldatcom/locatic:$ImageTag"
$NsApp = if ($EnvName -eq "prod") { "locatic-prod" } else { "locatic-staging" }
$AppOverlay = Join-Path $Root "deploy\k8s\app\overlays\$EnvName"
$NginxOverlay = Join-Path $Root "deploy\k8s\nginx\overlays\$EnvName"
$MonOverlay = Join-Path $Root "deploy\k8s\monitoring\overlays\$EnvName"

Write-Host "==> Ensure cluster context ($ClusterType)"
if ($ClusterType -eq "minikube") {
    minikube status 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Starting minikube..."
        minikube start --driver=docker --cpus=2 --memory=4096
    }
    kubectl config use-context minikube | Out-Null
}

& (Join-Path $PSScriptRoot "sync-kube-context.ps1") -EnvName $EnvName

Write-Host "==> Terraform init/apply ($EnvName)"
Push-Location $TfDir
try {
    terraform init -input=false
    terraform apply -auto-approve -input=false
    New-Item -ItemType Directory -Force -Path $AnsibleDir | Out-Null
    terraform output -json ansible_vars | Out-File -Encoding utf8 (Join-Path $AnsibleDir "vars.json")
}
finally {
    Pop-Location
}

Write-Host "==> Build + load image ($Image)"
Push-Location $Root
try {
    docker build -t $Image -t "ghcr.io/pauldatcom/locatic:latest" .
    if ($ClusterType -eq "minikube") {
        minikube image load $Image
    } else {
        kind load docker-image $Image --name kind
    }
}
finally {
    Pop-Location
}

Write-Host "==> Apply manifests (kubectl + Kustomize)"
kubectl apply -k $AppOverlay
kubectl apply -k $NginxOverlay
# Manifests referencent :latest ; on force le tag local charge dans le cluster.
kubectl -n $NsApp set image deployment/locatic "locatic=$Image"
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -
kubectl -n monitoring create secret generic grafana-admin-secret `
    --from-literal=GF_SECURITY_ADMIN_USER=admin `
    --from-literal=GF_SECURITY_ADMIN_PASSWORD=devops-training-local `
    --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -k $MonOverlay

Write-Host "==> Wait for rollouts"
kubectl -n $NsApp rollout status deployment/locatic --timeout=180s
kubectl -n $NsApp rollout status deployment/locatic-nginx --timeout=120s
kubectl -n monitoring rollout status deployment/prometheus --timeout=180s
kubectl -n monitoring rollout status deployment/grafana --timeout=120s

Write-Host "==> Done"
Write-Host "Image: $Image"
Write-Host "Verify:"
Write-Host "  .\scripts\verify.ps1"
Write-Host "  kubectl port-forward -n $NsApp svc/locatic-nginx 8888:80"
Write-Host "  curl http://127.0.0.1:8888/health"
Write-Host "  curl http://127.0.0.1:8888/metrics"
Write-Host ""
Write-Host "Linux/macOS: ./scripts/deploy.sh (Ansible)."
