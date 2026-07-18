#!/usr/bin/env bash
# Verifie que le deploiement Locatic sur minikube est fonctionnel.
#
# Controles :
#   1. Pods Locatic running et prets
#   2. PVC SQLite Bound
#   3. Endpoint /health repond 200 (via port-forward)
#   4. Endpoint /metrics renvoie des metriques Prometheus (via port-forward)
#   5. Acces via Nginx (point d'entree utilisateur) - port-forward Service nginx
#   6. Persistance SQLite : redemarrage d'un pod, donnees conservees
#   7. Targets Prometheus UP (namespace monitoring)
#
# Usage:
#   ./scripts/verify.sh [dev|prod]
#
# Par defaut : dev. Ce script est idempotent et non destructif (il ne redemarre
# un pod qu'avec votre accord explicite via --persistence-check).

set -euo pipefail

ENV="${1:-dev}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="${ROOT_DIR}/infra/terraform/environments/${ENV}"
RUN_PERSISTENCE_CHECK=false

for arg in "$@"; do
  case "$arg" in
    --persistence-check) RUN_PERSISTENCE_CHECK=true ;;
  esac
done

log() { printf '\033[1;34m[verify]\033[0m %s\n' "$*"; }
ok()  { printf '\033[1;32m  OK\033[0m %s\n' "$*"; }
err() { printf '\033[1;31m  KO\033[0m %s\n' "$*" >&2; }

command -v kubectl >/dev/null 2>&1 || { err "kubectl introuvable"; exit 1; }
minikube status >/dev/null 2>&1 || { err "minikube n'est pas demarre"; exit 1; }

APP_NS=$( ( cd "$TF_DIR" && terraform output -raw app_namespace ) 2>/dev/null || echo "locatic-${ENV}" )
[[ "$APP_NS" == locatic-* ]] || APP_NS="locatic-${ENV}"
MON_NS="monitoring"

log "Namespace applicatif : ${APP_NS}"
log "Namespace monitoring : ${MON_NS}"
echo

# --- 1. Pods Locatic ------------------------------------------------------
log "1/7 Pods Locatic"
if kubectl get pods -n "$APP_NS" -l app.kubernetes.io/name=locatic --no-headers \
    | awk '{print $3}' | grep -qv Running; then
  err "Certains pods ne sont pas Running"
  kubectl get pods -n "$APP_NS" -l app.kubernetes.io/name=locatic
else
  ok "Tous les pods Locatic sont Running"
  kubectl get pods -n "$APP_NS" -l app.kubernetes.io/name=locatic
fi
echo

# --- 2. PVC SQLite --------------------------------------------------------
log "2/7 PVC SQLite"
PVC_STATUS=$(kubectl get pvc -n "$APP_NS" locatic-sqlite -o jsonpath='{.status.phase}' 2>/dev/null || echo "NotFound")
if [[ "$PVC_STATUS" == "Bound" ]]; then
  ok "PVC locatic-sqlite est Bound"
else
  err "PVC locatic-sqlite n'est pas Bound (status=${PVC_STATUS})"
  kubectl get pvc -n "$APP_NS"
fi
echo

# --- 3. /health via port-forward -----------------------------------------
log "3/7 Endpoint /health"
PORT_FORWARD_PID=""
cleanup() {
  [[ -n "$PORT_FORWARD_PID" ]] && kill "$PORT_FORWARD_PID" 2>/dev/null || true
}
trap cleanup EXIT

kubectl port-forward -n "$APP_NS" svc/locatic 18080:8080 >/dev/null 2>&1 &
PORT_FORWARD_PID=$!
sleep 3

if curl -sf -o /dev/null -w "%{http_code}" http://localhost:18080/health | grep -q "^200$"; then
  ok "/health repond 200"
else
  err "/health ne repond pas 200"
  curl -sS -i http://localhost:18080/health || true
fi
kill "$PORT_FORWARD_PID" 2>/dev/null || true
PORT_FORWARD_PID=""
echo

# --- 4. /metrics via port-forward ---------------------------------------
log "4/7 Endpoint /metrics"
kubectl port-forward -n "$APP_NS" svc/locatic 18080:8080 >/dev/null 2>&1 &
PORT_FORWARD_PID=$!
sleep 3

if curl -sf http://localhost:18080/metrics | grep -q "^# HELP http_requests_total"; then
  ok "/metrics expose des metriques Prometheus (http_requests_total present)"
  curl -s http://localhost:18080/metrics | grep -E "^(http_requests_total|process_start_time_seconds)" | head -5
else
  err "/metrics ne renvoie pas de metriques Prometheus valides"
  curl -sS http://localhost:18080/metrics | head -10 || true
fi
kill "$PORT_FORWARD_PID" 2>/dev/null || true
PORT_FORWARD_PID=""
echo

# --- 5. Acces via Nginx ---------------------------------------------------
log "5/7 Acces via Nginx (point d'entree utilisateur)"
# On suppose que Gires deploie Nginx dans le meme namespace, Service nomme
# 'nginx' de type NodePort. Si le nom differe, ajuster ici.
NGINX_SVC=$(kubectl get svc -n "$APP_NS" nginx -o jsonpath='{.spec.ports[0].nodePort}' 2>/dev/null || echo "")
if [[ -n "$NGINX_SVC" ]]; then
  MINIKUBE_IP=$(minikube ip 2>/dev/null || echo "127.0.0.1")
  HTTP_CODE=$(curl -sf -o /dev/null -w "%{http_code}" "http://${MINIKUBE_IP}:${NGINX_SVC}/" || echo "000")
  if [[ "$HTTP_CODE" =~ ^2|^3 ]]; then
    ok "Acces via Nginx (http://${MINIKUBE_IP}:${NGINX_SVC}/) -> ${HTTP_CODE}"
  else
    err "Nginx ne repond pas correctement (HTTP ${HTTP_CODE})"
  fi
else
  err "Service 'nginx' introuvable dans ${APP_NS} (la partie Nginx de Gires n'est peut-etre pas deployee)"
fi
echo

# --- 6. Persistance SQLite ------------------------------------------------
log "6/7 Persistance SQLite"
if kubectl exec -n "$APP_NS" deploy/locatic -- ls /data/agence.db >/dev/null 2>&1; then
  ok "Le fichier /data/agence.db existe dans le conteneur"
  BEFORE=$(kubectl exec -n "$APP_NS" deploy/locatic -- stat -c '%Y' /data/agence.db 2>/dev/null || echo "?")
  log "mtime du fichier avant redemarrage : ${BEFORE}"
  if [[ "$RUN_PERSISTENCE_CHECK" == "true" ]]; then
    log "Redemarrage du pod (rollout restart)..."
    kubectl rollout restart deploy/locatic -n "$APP_NS"
    kubectl rollout status deploy/locatic -n "$APP_NS" --timeout=180s
    sleep 3
    AFTER=$(kubectl exec -n "$APP_NS" deploy/locatic -- stat -c '%Y' /data/agence.db 2>/dev/null || echo "?")
    log "mtime du fichier apres redemarrage : ${AFTER}"
    if [[ "$BEFORE" == "$AFTER" && "$BEFORE" != "?" ]]; then
      ok "SQLite a survecu au redemarrage (mtime identique)"
    else
      err "SQLite a change pendant le redemarrage (avant=${BEFORE} apres=${AFTER})"
    fi
  else
    log "Pour tester la persistance au redemarrage, relancer avec --persistence-check"
  fi
else
  err "/data/agence.db introuvable (la base n'est pas encore initialisee ?)"
fi
echo

# --- 7. Prometheus targets -----------------------------------------------
log "7/7 Targets Prometheus"
PROM_SVC=$(kubectl get svc -n "$MON_NS" prometheus -o jsonpath='{.spec.ports[0].port}' 2>/dev/null || echo "")
if [[ -n "$PROM_SVC" ]]; then
  kubectl port-forward -n "$MON_NS" svc/prometheus 19090:"${PROM_SVC}" >/dev/null 2>&1 &
  PORT_FORWARD_PID=$!
  sleep 3
  UP_COUNT=$(curl -sf http://localhost:19090/api/v1/targets \
    | grep -o '"health":"up"' | wc -l | tr -d ' ' || echo "0")
  ok "Prometheus reachable, ${UP_COUNT} target(s) UP"
  kill "$PORT_FORWARD_PID" 2>/dev/null || true
  PORT_FORWARD_PID=""
else
  err "Service 'prometheus' introuvable dans ${MON_NS} (la partie monitoring de Gires n'est peut-etre pas deployee)"
fi
echo

log "Verification terminee."