# Deploiement local — Locatic

Chaine complete pour passer du code local a une application accessible sur un
cluster Kubernetes local, avec monitoring. Portable pour tous les membres du
groupe et pour la soutenance.

## Vue d'ensemble

```txt
Terraform                  Deploy                         Cluster local
---------                  ------                         -------------
namespaces                 docker build                   locatic-staging
PVC sqlite                 image load (minikube|kind)     app + nginx
namespace monitoring       kubectl apply -k deploy/k8s/*  monitoring
```

## Chemin recommande (1 commande)

### Windows (Docker Desktop + minikube)

```powershell
.\scripts\setup-prereqs.ps1   # une fois : minikube, terraform, ansible WSL
.\scripts\deploy.ps1          # terraform + build + load + apply
.\scripts\verify.ps1          # health, /metrics, cibles Prometheus
```

### Linux / macOS

```bash
./scripts/setup-prereqs.sh    # si besoin
./scripts/deploy.sh dev minikube
./scripts/verify.sh
```

`deploy.sh` orchestre Terraform puis Ansible. Sur Windows, preferer `deploy.ps1`
(kubectl natif) pour eviter les soucis de chemin / kubeconfig entre WSL et minikube.

## Prerequis

- Docker Desktop (ou Docker Engine)
- **minikube** (consigne du mini-projet) — kind en alternative
- kubectl, Terraform >= 1.6
- Ansible + collection `kubernetes.core` (surtout pour le chemin Linux)

```bash
docker --version
minikube status
kubectl config current-context
terraform --version
```

## Etape manuelle (si tu ne veux pas les scripts)

### 1 — Cluster

```bash
minikube start --driver=docker --cpus=2 --memory=4096
kubectl config use-context minikube
```

### 2 — Terraform

```bash
cd infra/terraform/environments/dev
# kube_context = "minikube" dans terraform.tfvars
terraform init
terraform apply -auto-approve
```

Cree : `locatic-staging`, `monitoring`, PVC `locatic-sqlite`.

### 3 — Image + manifests

```bash
# Tag unique obligatoire en local (sinon minikube peut garder une vieille :latest)
TAG=local-$(date +%Y%m%d%H%M%S)
IMAGE=ghcr.io/pauldatcom/locatic:$TAG
docker build -t "$IMAGE" .
minikube image load "$IMAGE"

kubectl apply -k deploy/k8s/app/overlays/dev
kubectl apply -k deploy/k8s/nginx/overlays/dev
kubectl -n locatic-staging set image deployment/locatic "locatic=$IMAGE"

kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -
kubectl -n monitoring create secret generic grafana-admin-secret \
  --from-literal=GF_SECURITY_ADMIN_USER=admin \
  --from-literal=GF_SECURITY_ADMIN_PASSWORD=devops-training-local \
  --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -k deploy/k8s/monitoring/overlays/dev
```

Ou via Ansible :

```bash
cd infra/ansible
ansible-galaxy collection install -r requirements.yml
ansible-playbook -i inventory.yml deploy-k8s.yml
```

## Verification

```bash
kubectl get all,pvc -n locatic-staging
kubectl get all -n monitoring

kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
curl http://127.0.0.1:8888/health
curl http://127.0.0.1:8888/metrics
```

Monitoring :

```bash
kubectl port-forward -n monitoring svc/prometheus 9090:9090
kubectl port-forward -n monitoring svc/grafana 3000:3000
# Grafana : admin / devops-training-local
```

## Depannage

| Symptome | Solution |
| --- | --- |
| `context "X" does not exist` | Aligner `kube_context` dans `terraform.tfvars` (`.\scripts\sync-kube-context.ps1`) |
| App `CreateContainerConfigError` / UID | Image avec `USER 999` + `runAsUser: 999` dans le Deployment |
| `/metrics` 404 alors que le code est a jour | Tag image unique + `kubectl set image` (cache `:latest` minikube) |
| Nginx CrashLoop / probe 502 | Probe sur `/ready` (locale), pas `/health` (depend du backend) |
| ConfigMap Prometheus invalide | Cle `alerts.yml` (pas `rules/alerts.yml`) |
| PVC `Pending` sans pod | Normal avec `WaitForFirstConsumer` — Bound apres demarrage app |
| Port 8080 local = Adminer | Utiliser `8888` via Nginx ou un autre port (`18080`) |
| Ansible WSL ne voit pas minikube | Sur Windows : `.\scripts\deploy.ps1` |
