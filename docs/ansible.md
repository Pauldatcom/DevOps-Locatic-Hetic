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
├── site.yml                   # Legacy (VM) - non utilise pour K8s
├── bootstrap-python.yml       # Legacy
└── roles/
    ├── k8s_deploy/            # Build image + apply deploy/k8s/*
    ├── base/                  # Legacy VM
    └── ngnix/                 # Legacy VM (typo historique du role)
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
| `locatic_image_name` | `locatic:latest` | Image buildée localement |
| `locatic_docker_context` | racine du repo | Chemin portable via `playbook_dir` |
| `k8s_cluster_type` | `minikube` | `minikube` ou `kind` |
| `kind_cluster_name` | `kind` | Nom du cluster kind |
| `k8s_environment` | `dev` | Overlay Kustomize (`dev` / `prod`) |
| `k8s_namespace` | `locatic-staging` | Namespace applicatif |
| `deploy_monitoring` | `true` | Appliquer aussi Prometheus/Grafana |

## Ce que fait `k8s_deploy`

1. Verifie Docker + kubectl + cluster
2. Build l'image Docker Locatic
3. Charge l'image dans minikube (`minikube image load`) ou kind
4. Applique `deploy/k8s/app/overlays/<env>`
5. Applique `deploy/k8s/nginx/overlays/<env>`
6. Applique `deploy/k8s/monitoring/overlays/<env>` (si active)
7. Attend que les Deployments `locatic` et `locatic-nginx` soient Available

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

## Roles legacy

`base`, `ngnix`, `site.yml` et `bootstrap-python.yml` viennent d'un ancien
parcours VM. Ils ne sont **pas** utilises pour le deploiement Kubernetes Locatic.
