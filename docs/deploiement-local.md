# Deploiement local — Locatic

Chaine complete pour passer d'une image publiee / code local a une application
accessible sur un cluster Kubernetes local, avec monitoring.

## Vue d'ensemble

```txt
Terraform                  Ansible                         Cluster local
---------                  -------                         -------------
namespaces                 docker build                    locatic-staging
PVC sqlite                 image load (minikube|kind)      app + nginx
namespace monitoring       kubectl apply -k deploy/k8s/*   monitoring
```

- **Terraform** : namespaces + PVC SQLite
- **Ansible** : build image, load, apply app + Nginx + monitoring
- **Point d'entree utilisateur** : Service `locatic-nginx` (NodePort / port-forward)

## Prerequis

- Docker Desktop
- **minikube** (consigne du mini-projet) — ou kind en alternative
- kubectl, Terraform >= 1.6, Ansible + `kubernetes.core`
- Python module `kubernetes`

```bash
docker --version
minikube status          # ou : kind get clusters
kubectl config current-context
terraform --version
ansible --version
```

## Etape 1 — Demarrer le cluster

```bash
minikube start
kubectl config use-context minikube
```

Alternative kind :

```bash
kind create cluster --name kind
kubectl config use-context kind-kind
```

## Etape 2 — Infrastructure Terraform

```bash
cd infra/terraform/environments/dev
# Verifier kube_context dans terraform.tfvars (minikube ou kind-kind)
terraform init
terraform plan
terraform apply
```

Ressources creees :

- `locatic-staging`
- `monitoring`
- PVC `locatic-sqlite`

```bash
kubectl get ns
kubectl get pvc -n locatic-staging
```

## Etape 3 — Deploiement Ansible

```bash
cd infra/ansible
ansible-galaxy collection install -r requirements.yml
pip install kubernetes

# minikube (defaut)
ansible-playbook -i inventory.yml deploy-k8s.yml

# kind
ansible-playbook -i inventory.yml deploy-k8s.yml -e k8s_cluster_type=kind
```

Ansible applique :

- `deploy/k8s/app/overlays/dev`
- `deploy/k8s/nginx/overlays/dev`
- `deploy/k8s/monitoring/overlays/dev`

## Etape 4 — Verification

```bash
kubectl get all,pvc -n locatic-staging
kubectl get all -n monitoring

kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
curl http://localhost:8888/health
curl http://localhost:8888/metrics
```

Monitoring :

```bash
kubectl port-forward -n monitoring svc/prometheus 9090:9090
kubectl port-forward -n monitoring svc/grafana 3000:3000
# Grafana : admin / devops-training-local (voir docs/monitoring.md)
```

## Ordre complet

```bash
minikube start
cd infra/terraform/environments/dev && terraform init && terraform apply
cd ../../../ansible
ansible-galaxy collection install -r requirements.yml
ansible-playbook -i inventory.yml deploy-k8s.yml
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
```

## Depannage

| Symptome | Solution |
| --- | --- |
| `context "X" does not exist` | Aligner `kube_context` dans `terraform.tfvars` |
| `minikube` introuvable | Installer minikube ou utiliser `-e k8s_cluster_type=kind` |
| PVC `Pending` sans pod | Normal avec `WaitForFirstConsumer` — Bound apres deploy app |
| Target Prometheus `locatic-app` DOWN | Verifier `/metrics` et annotations sur le Deployment `locatic` |
| Chemin Docker invalide | Les defaults Ansible utilisent `playbook_dir` (plus de chemin machine) |
