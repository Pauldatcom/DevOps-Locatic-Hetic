#!/usr/bin/env bash
# Deploy Locatic locally: Terraform then Ansible.
# Usage: ./scripts/deploy.sh [dev|prod] [minikube|kind]
set -euo pipefail

ENV_NAME="${1:-dev}"
CLUSTER_TYPE="${2:-minikube}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="${ROOT_DIR}/infra/terraform/environments/${ENV_NAME}"
ANSIBLE_DIR="${ROOT_DIR}/infra/ansible"

if [[ ! -d "${TF_DIR}" ]]; then
  echo "Unknown environment: ${ENV_NAME} (expected dev or prod)" >&2
  exit 1
fi

echo "==> Terraform init/apply (${ENV_NAME})"
cd "${TF_DIR}"
terraform init -input=false
terraform apply -auto-approve -input=false

echo "==> Export Terraform outputs for Ansible"
mkdir -p "${ANSIBLE_DIR}"
terraform output -json ansible_vars > "${ANSIBLE_DIR}/vars.json"

echo "==> Ansible deploy (cluster=${CLUSTER_TYPE})"
cd "${ANSIBLE_DIR}"
ansible-galaxy collection install -r requirements.yml >/dev/null
ansible-playbook -i inventory.yml deploy-k8s.yml \
  -e "k8s_cluster_type=${CLUSTER_TYPE}" \
  -e "k8s_environment=${ENV_NAME}"

echo "==> Done"
echo "App:        kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80"
echo "Prometheus: kubectl port-forward -n monitoring svc/prometheus 9090:9090"
echo "Grafana:    kubectl port-forward -n monitoring svc/grafana 3000:3000"
