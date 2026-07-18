#!/usr/bin/env bash
# Orchestre le deploiement local complet de Locatic sur minikube.
#
# Sequence (voir docs/deploiement-local.md) :
#   1. preconditions (minikube, kubectl, terraform, ansible installes)
#   2. terraform init + apply (cree namespaces + PVC SQLite)
#   3. terraform output -json ansible_vars -> infra/ansible/vars.json
#   4. ansible-playbook (applique manifests app + nginx + monitoring)
#   5. kubectl rollout status (attend que les pods soient prets)
#
# Usage:
#   ./scripts/deploy.sh [dev|prod]
#
# Par defaut : dev.

set -euo pipefail

ENV="${1:-dev}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="${ROOT_DIR}/infra/terraform/environments/${ENV}"
ANSIBLE_DIR="${ROOT_DIR}/infra/ansible"
ANSIBLE_VARS="${ANSIBLE_DIR}/vars.json"

log() { printf '\033[1;34m[deploy]\033[0m %s\n' "$*"; }
err()  { printf '\033[1;31m[deploy]\033[0m %s\n' "$*" >&2; }

# --- Preconditions -------------------------------------------------------
check_cmd() {
  command -v "$1" >/dev/null 2>&1 || { err "$1 introuvable (installez-le avant de relancer)"; exit 1; }
}
check_cmd minikube
check_cmd kubectl
check_cmd terraform
check_cmd ansible-playbook

if [[ ! -d "$TF_DIR" ]]; then
  err "Environnement inconnu : ${ENV} (dossier absent : ${TF_DIR})"
  exit 1
fi

log "Contexte Kubernetes actuel : $(kubectl config current-context 2>/dev/null || echo 'indefini')"
minikube status >/dev/null 2>&1 || { err "minikube n'est pas demarre. Lancez 'minikube start'."; exit 1; }
log "minikube OK"

# --- 1. Terraform --------------------------------------------------------
log "1/4 Terraform init/apply dans ${TF_DIR}"
(
  cd "$TF_DIR"
  terraform init -input=false
  terraform validate
  terraform apply -auto-approve
)

log "2/4 Ecriture des outputs Terraform vers ${ANSIBLE_VARS}"
( cd "$TF_DIR" && terraform output -json ansible_vars ) \
  | ( command -v jq >/dev/null 2>&1 && jq '.' || cat ) \
  > "$ANSIBLE_VARS"
log "Variables Ansible ecrites :"
sed 's/^/    /' "$ANSIBLE_VARS"

# --- 2. Ansible ----------------------------------------------------------
log "3/4 ansible-playbook (deploiement app + nginx + monitoring)"
ansible-playbook "${ANSIBLE_DIR}/playbook.yml" \
  --extra-vars "@${ANSIBLE_VARS}" \
  --extra-vars "environment=${ENV}"

# --- 3. Rollout status ---------------------------------------------------
# Le namespace applicatif est lu depuis les outputs Terraform (via jq si dispo).
APP_NS=$( ( cd "$TF_DIR" && terraform output -raw app_namespace ) 2>/dev/null || echo "locatic-${ENV}" )
[[ "$APP_NS" == "locatic-staging" ]] || [[ "$APP_NS" == locatic-* ]] || APP_NS="locatic-${ENV}"

log "4/4 kubectl rollout status deploy/locatic dans ${APP_NS}"
kubectl rollout status deploy/locatic -n "$APP_NS" --timeout=180s \
  || { err "Le Deployment Locatic n'est pas pret"; exit 1; }

log "Deploiement termine avec succes."
log "  Namespace app      : ${APP_NS}"
log "  Namespace monit.   : monitoring"
log "  Point d'entree     : voir docs/deploiement-local.md (port Nginx)"
log "  Verification       : ./scripts/verify.sh ${ENV}"