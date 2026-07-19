# Deploy Locatic locally on Windows: Terraform then Ansible.
# Usage: .\scripts\deploy.ps1 [-EnvName dev] [-ClusterType minikube]
param(
    [ValidateSet("dev", "prod")]
    [string]$EnvName = "dev",
    [ValidateSet("minikube", "kind")]
    [string]$ClusterType = "minikube"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$TfDir = Join-Path $Root "infra\terraform\environments\$EnvName"
$AnsibleDir = Join-Path $Root "infra\ansible"

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

Write-Host "==> Ansible deploy (cluster=$ClusterType)"
Push-Location $AnsibleDir
try {
    ansible-galaxy collection install -r requirements.yml | Out-Null
    ansible-playbook -i inventory.yml deploy-k8s.yml `
        -e "k8s_cluster_type=$ClusterType" `
        -e "k8s_environment=$EnvName"
}
finally {
    Pop-Location
}

Write-Host "==> Done"
Write-Host "App:        kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80"
Write-Host "Prometheus: kubectl port-forward -n monitoring svc/prometheus 9090:9090"
Write-Host "Grafana:    kubectl port-forward -n monitoring svc/grafana 3000:3000"
