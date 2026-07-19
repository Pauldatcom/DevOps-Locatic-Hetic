# Kubernetes

L'application Locatic est deployee sur un cluster Kubernetes local minikube.
Ce document decrit les ressources utilisees, le reverse proxy Nginx, le stockage
SQLite et les metriques Prometheus.

## Structure canonique

Tous les manifests de deploiement vivent sous `deploy/k8s/` :

```txt
deploy/k8s/
├── app/                       # Application Locatic (+ /metrics)
│   ├── base/
│   │   ├── deployment.yaml
│   │   ├── service.yaml       # ClusterIP (interne)
│   │   ├── configmap.yaml
│   │   ├── secret.yaml.tpl    # Template - jamais de vrai secret
│   │   └── kustomization.yaml
│   └── overlays/
│       ├── dev/               # namespace locatic-staging
│       └── prod/              # namespace locatic-prod
└── nginx/                     # Reverse proxy (point d'entree)
    ├── base/
    │   ├── deployment.yaml
    │   ├── service.yaml       # NodePort
    │   ├── configmap.yaml
    │   └── kustomization.yaml
    └── overlays/
        ├── dev/
        └── prod/
```

## Architecture

```txt
Utilisateur
  -> Service locatic-nginx (NodePort)
  -> Pod Nginx
  -> Service locatic (ClusterIP :8080)
  -> Pod Locatic (/health, /metrics)
  -> PVC locatic-sqlite (/data)
```

L'application n'est **pas** exposee directement. Nginx est le point d'entree.

## Separation des responsabilites

| Ressource | Outil |
| --- | --- |
| Namespaces (`locatic-staging`, `locatic-prod`, `monitoring`) | Terraform |
| PVC `locatic-sqlite` | Terraform |
| Deployment / Service / ConfigMap app | `deploy/k8s/app` (+ Ansible) |
| Deployment / Service / ConfigMap Nginx | `deploy/k8s/nginx` (+ Ansible) |
| Stack Prometheus / Grafana | Monitoring + Ansible |

## Alignement Terraform

| Element | Dev | Prod |
| --- | --- | --- |
| Namespace | `locatic-staging` | `locatic-prod` |
| PVC | `locatic-sqlite` | `locatic-sqlite` |
| Mount path | `/data` | `/data` |
| Image | `ghcr.io/pauldatcom/locatic:<tag>` | SHA epingle recommande |
| Replicas app | `1` (SQLite ReadWriteOnce) | `1` |

Strategie Deployment app : `Recreate` (evite deux pods sur la meme PVC RWO).

## Metriques

L'application expose `/metrics` via `prometheus-net` (port 8080).
Des annotations `prometheus.io/*` sont presentes sur le pod applicatif pour
preparer le scrape Prometheus (partie monitoring).

## Secret

`secret.yaml.tpl` est un **template**. Ne jamais y committer de vrai secret.
Le Secret `locatic-secret` est optionnel au demarrage (`optional: true`) et
doit etre valorise par Ansible si des variables sensibles apparaissent.

## Application

Apres `terraform apply` :

```bash
# Application
kubectl apply -k deploy/k8s/app/overlays/dev

# Reverse proxy Nginx
kubectl apply -k deploy/k8s/nginx/overlays/dev
```

Verification :

```bash
kubectl get all,pvc -n locatic-staging
kubectl logs deploy/locatic -n locatic-staging
kubectl logs deploy/locatic-nginx -n locatic-staging
kubectl port-forward svc/locatic-nginx 8080:80 -n locatic-staging
curl http://localhost:8080/health
curl http://localhost:8080/metrics
```

## Probes et ressources

- App : startup / liveness / readiness sur `/health`
- Nginx : liveness / readiness via `/health` proxifie vers l'app
- Requests / limits CPU et memoire definies pour app et Nginx
