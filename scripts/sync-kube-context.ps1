# Sync terraform.tfvars kube_context with the current kubectl context.
param(
    [ValidateSet("dev", "prod")]
    [string]$EnvName = "dev"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Tfvars = Join-Path $Root "infra\terraform\environments\$EnvName\terraform.tfvars"

$ctx = (kubectl config current-context).Trim()
if (-not $ctx) { throw "No kubectl current-context. Start minikube first." }

$content = Get-Content $Tfvars -Raw
if ($content -match 'kube_context\s*=\s*"[^"]*"') {
    $content = $content -replace 'kube_context\s*=\s*"[^"]*"', "kube_context    = `"$ctx`""
} else {
    $content += "`nkube_context    = `"$ctx`"`n"
}
Set-Content -Path $Tfvars -Value $content -NoNewline
Write-Host "Updated $Tfvars -> kube_context = $ctx"
