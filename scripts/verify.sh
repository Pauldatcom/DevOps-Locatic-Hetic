#!/usr/bin/env bash
# Quick post-deploy verification helpers.
set -euo pipefail

NS_APP="${1:-locatic-staging}"
NS_MON="${2:-monitoring}"

echo "==> Resources in ${NS_APP}"
kubectl get all,pvc -n "${NS_APP}"

echo "==> Resources in ${NS_MON}"
kubectl get all,pvc -n "${NS_MON}"

echo "==> Health via Nginx (port-forward in background)"
kubectl port-forward -n "${NS_APP}" svc/locatic-nginx 8888:80 >/tmp/locatic-pf-nginx.log 2>&1 &
PF_PID=$!
sleep 2
curl -fsS "http://127.0.0.1:8888/health" && echo
curl -fsS "http://127.0.0.1:8888/metrics" | head -n 5 || true
kill "${PF_PID}" >/dev/null 2>&1 || true

echo "==> Prometheus targets (open UI manually)"
echo "kubectl port-forward -n ${NS_MON} svc/prometheus 9090:9090"
echo "Then open http://localhost:9090/targets"
