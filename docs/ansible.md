# Ansible — Locatic

Ansible orchestre le deploiement local de Locatic sur un cluster Kubernetes
(**minikube** par defaut, **kind** en alternative), apres Terraform
(namespaces + PVC SQLite).

## Structure

```txt
infra/ansible/
├── inventory.yml
├── requirements.yml
├── deploy-k8s.yml             # Playbook principal
└── roles/
    └── k8s_deploy/            # Build image + apply deploy/k8s/*
```

## Prerequis

- Docker Desktop
- Cluster local demarre : `minikube start` (consigne) ou `kind create cluster`
- `kubectl` pointe vers le bon contexte
- Ansible + collection `kubernetes.core`
- Python module `kubernetes` (`pip install kubernetes`)

```bash
cd infra/ansible
ansible-galaxy collection install -r requirements.yml
pip install kubernetes
```

## Variables (`roles/k8s_deploy/defaults/main.yml`)

| Variable | Default | Description |
| --- | --- | --- |
| `locatic_image_name` | `ghcr.io/pauldatcom/locatic:local-...` | Image buildée localement (tag unique) |
| `locatic_docker_context` | racine du repo | Chemin portable via `playbook_dir` |
| `k8s_cluster_type` | `minikube` | `minikube` ou `kind` |
| `kind_cluster_name` | `kind` | Nom du cluster kind |
| `k8s_environment` | `dev` | Overlay Kustomize (`dev` / `prod`) |
| `k8s_namespace` | `locatic-staging` | Namespace applicatif |
| `deploy_monitoring` | `true` | Appliquer aussi Prometheus/Grafana |

## Ce que fait `k8s_deploy`

1. Verifie Docker + kubectl (+ Helm si `use_helm=true`) + cluster
2. Build l'image Docker Locatic
3. Charge l'image dans minikube (`minikube image load`) ou kind
4. Deploye app + Nginx via **Helm** (`deploy/helm/locatic`) — ou Kustomize en fallback
5. Applique `deploy/k8s/monitoring/overlays/<env>` (si active)
6. Attend que les Deployments `locatic` et `locatic-nginx` soient Available

## Lancer

Apres `terraform apply` :

```bash
cd infra/ansible

# Defaut = minikube
ansible-playbook -i inventory.yml deploy-k8s.yml

# Alternative kind
ansible-playbook -i inventory.yml deploy-k8s.yml -e k8s_cluster_type=kind
```

## Verification

```bash
kubectl get all,pvc -n locatic-staging
kubectl get all -n monitoring
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
curl http://localhost:8888/health
curl http://localhost:8888/metrics
```
