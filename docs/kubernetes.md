# Kubernetes

Ce dossier documente la partie Kubernetes du mini-projet Locatic. Les manifests
versionnes se trouvent dans `infra/kubernetes/base` et sont prevus pour etre
appliques sur le cluster minikube local **apres** Terraform (namespaces + PVC).

## Separation des responsabilites

| Ressource | Outil | Detail |
| --- | --- | --- |
| Namespace app (`locatic-staging` / `locatic-prod`) | Terraform | Module `namespace` |
| Namespace `monitoring` | Terraform | Module `namespace` |
| PVC SQLite (`locatic-sqlite`) | Terraform | Module `storage` |
| ConfigMaps, Deployments, Services | Kubernetes manifests / Ansible | `infra/kubernetes/base` |

Les manifests Kubernetes ne recreent plus le PVC : ils le referencent seulement.

## Ressources fournies

- `configmap.yaml` : configuration ASP.NET Core et reverse proxy Nginx.
- `app.yaml` : Deployment Locatic + Service interne `ClusterIP`.
- `nginx.yaml` : Deployment Nginx + Service `NodePort` (point d'entree).
- `kustomization.yaml` : regroupe les manifests et fixe le namespace
  `locatic-staging` (aligné avec Terraform `environments/dev`).

## Architecture Kubernetes

```txt
Utilisateur -> Service locatic-nginx -> Pod Nginx -> Service locatic-app -> Pod Locatic -> PVC locatic-sqlite (/data)
```

Le service `locatic-app` reste en `ClusterIP`. Le service `locatic-nginx` est le
point d'entree principal expose via `NodePort` pour minikube.

## Alignement avec Terraform

| Element | Valeur Terraform (dev) | Manifests K8s |
| --- | --- | --- |
| Namespace | `locatic-staging` | `locatic-staging` |
| PVC | `locatic-sqlite` | `claimName: locatic-sqlite` |
| Mount path | `/data` | `/data` |
| Image | `ghcr.io/pauldatcom/locatic:latest` | meme image par defaut |
| Replicas app | variable `app_replicas` | `1` (SQLite ReadWriteOnce) |

En production, le namespace devient `locatic-prod`. Ansible peut surcharger le
namespace Kustomize et le tag d'image via les outputs Terraform (`ansible_vars`).

> Note : Terraform propose `app_replicas = 2` en dev. Pour SQLite en
> `ReadWriteOnce`, les manifests gardent volontairement **1 replica** et une
> strategie `Recreate`.

## Configuration applicative

ConfigMap `locatic-app-config` :

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection=Data Source=/data/agence.db`

Le volume `/data` est monte depuis la PVC Terraform `locatic-sqlite`.

## Image et tag

```bash
kubectl set image deployment/locatic-app locatic=ghcr.io/pauldatcom/locatic:<tag> -n locatic-staging
```

Avec Kustomize (apres `terraform apply`) :

```bash
kubectl kustomize infra/kubernetes/base
kubectl apply -k infra/kubernetes/base
```

## Probes et ressources

Le pod applicatif expose `/health`. Nginx proxifie aussi `/health` : si l'app
ne repond plus, l'entree Nginx devient non prete.

Requests et limits CPU/memoire sont definies pour l'application et Nginx.

## Verification locale

```bash
kubectl get all -n locatic-staging
kubectl get pvc -n locatic-staging
kubectl describe deployment locatic-app -n locatic-staging
kubectl describe deployment locatic-nginx -n locatic-staging
kubectl logs deploy/locatic-app -n locatic-staging
kubectl logs deploy/locatic-nginx -n locatic-staging
minikube service locatic-nginx -n locatic-staging
```

Acces via Nginx :

```bash
kubectl port-forward svc/locatic-nginx 8080:80 -n locatic-staging
curl http://localhost:8080/health
```

## Monitoring

Annotations Prometheus sur le pod/service applicatif pour preparer `/metrics`.
La stack Prometheus/Grafana reste orchestree par Ansible.
