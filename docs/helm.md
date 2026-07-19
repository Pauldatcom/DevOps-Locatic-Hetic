# Helm — Locatic

## Statut

Bonus **realise** : chart Helm configurable pour l'application + Nginx,
installe/mis a jour par **Ansible** (`helm upgrade --install`).

Le monitoring (Prometheus/Grafana) reste en manifests Kustomize
(`deploy/k8s/monitoring/`).

## Structure

```txt
deploy/helm/locatic/
├── Chart.yaml
├── values.yaml              # Defauts
├── values-dev.yaml          # namespace locatic-staging, log debug
├── values-prod.yaml         # namespace locatic-prod
└── templates/
    ├── _helpers.tpl
    ├── app-configmap.yaml
    ├── app-deployment.yaml
    ├── app-service.yaml
    ├── nginx-configmap.yaml
    ├── nginx-deployment.yaml
    └── nginx-service.yaml
```

## Valeurs configurables

| Cle | Description |
| --- | --- |
| `image.repository` / `image.tag` | Image Locatic |
| `app.replicas` | Replicas (1 pour SQLite RWO) |
| `app.logLevel` | Niveau de log |
| `app.pvcName` | PVC SQLite (creee par Terraform) |
| `nginx.serviceType` | NodePort / ClusterIP |
| `nginx.enabled` | Activer le reverse proxy |

## Procedure

```bash
# Lint
helm lint deploy/helm/locatic

# Install / upgrade (dev)
helm upgrade --install locatic deploy/helm/locatic \
  -n locatic-staging \
  -f deploy/helm/locatic/values-dev.yaml \
  --set image.repository=ghcr.io/pauldatcom/locatic \
  --set image.tag=latest \
  --wait

# Statut / historique
helm status locatic -n locatic-staging
helm history locatic -n locatic-staging

# Rollback
helm rollback locatic 1 -n locatic-staging
```

## Ansible

Par defaut `use_helm: true` dans `roles/k8s_deploy/defaults/main.yml`.

```bash
cd infra/ansible
ansible-playbook -i inventory.yml deploy-k8s.yml
# Force Kustomize a la place de Helm :
ansible-playbook -i inventory.yml deploy-k8s.yml -e use_helm=false
```

## Relation avec Kustomize

- **Helm** : chemin principal pour app + Nginx (bonus).
- **Kustomize** (`deploy/k8s/app`, `deploy/k8s/nginx`) : conserve comme fallback
  et reference manifeste.
- La PVC SQLite reste geree par **Terraform** (pas dans le chart).
