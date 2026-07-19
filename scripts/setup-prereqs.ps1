# Install prerequisites on Windows for Locatic DevOps project.
# Run in an elevated PowerShell if winget asks for elevation.
param(
    [switch]$SkipWinget
)

$ErrorActionPreference = "Stop"

Write-Host "==> Checking Docker Desktop"
docker version | Out-Null

if (-not $SkipWinget) {
    Write-Host "==> Installing minikube + Terraform via winget (if missing)"
    winget install -e --id Kubernetes.minikube --accept-package-agreements --accept-source-agreements
    winget install -e --id Hashicorp.Terraform --accept-package-agreements --accept-source-agreements
}

$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path", "User")

Write-Host "==> Tool versions"
minikube version
terraform version
kubectl version --client

Write-Host "==> Preparing Ansible in WSL Ubuntu"
$repoWin = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$repoWsl = wsl -d Ubuntu -- wslpath -a "$repoWin"
wsl -d Ubuntu -- bash -lc "pip3 install --user kubernetes >/dev/null 2>&1 || true; ansible-galaxy collection install kubernetes.core -f >/dev/null; ansible --version | head -n 1"

Write-Host ""
Write-Host "Prereqs ready. Next:"
Write-Host "  .\scripts\deploy.ps1 -EnvName dev -ClusterType minikube"
Write-Host "Repo WSL path: $repoWsl"
