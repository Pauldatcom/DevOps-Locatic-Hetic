# Architecture

Ce document decrit l'architecture DevOps du projet Locatic : roles de GitHub,
CI, Terraform, Ansible, Kubernetes, Nginx, SQLite et monitoring.

## Schema cible

Le schema officiel du mini-projet est dans
[`docs/assets/mini-project-architecture.png`](assets/mini-project-architecture.png).

```txt
Developpeur
    |
    v
GitHub (PR + checks) ---- GitHub Actions ---- GHCR (image)
    |
    | (apres merge / image publiee)
    v
Machine locale
    |
    +--> Terraform : namespaces + PVC SQLite
    |
    +--> Ansible   : build/load image + apply deploy/k8s/*
    |
    v
minikube
    |
    +--> Nginx (NodePort) --> App Locatic (ClusterIP) --> PVC SQLite
    |
    +--> Prometheus / Grafana / Alertmanager / Node Exporter
```

## Role de chaque composant

### GitHub

Point central du projet : code, Pull Requests, protection de `main`, historique
lisible. Aucun secret reel ni `terraform.tfstate` ne doit y etre versionne.

### GitHub Actions

Execute les controles automatiques sur PR et `main` :

- restore / build / tests .NET
- lint (`dotnet format`)
- scan Trivy (filesystem + image)
- publication de l'image sur `ghcr.io` **uniquement sur push `main`**

Le pipeline **ne deploie pas** sur minikube.

### Terraform

Prepare l'infrastructure locale minikube :

- namespaces `locatic-staging` / `locatic-prod` et `monitoring`
- PVC `locatic-sqlite`
- outputs (`ansible_vars`) pour Ansible / `scripts/deploy.sh`

### Helm (bonus)

- Chart `deploy/helm/locatic` : Deployment/Service/ConfigMap app + Nginx
- Installe via Ansible (`helm upgrade --install`) ou `scripts/deploy.ps1`
- Rollback : `helm rollback locatic <revision> -n locatic-staging`

### Ansible

Orchestre le deploiement local :

- build Docker
- load image dans minikube (ou kind)
- `kubectl apply -k` sur `deploy/k8s/{app,nginx,monitoring}`
- creation du Secret Grafana (non versionne)

### Kubernetes (`deploy/k8s/`)

- `app/` : Deployment Locatic + Service ClusterIP + ConfigMap
- `nginx/` : reverse proxy NodePort (point d'entree utilisateur)
- `monitoring/` : Prometheus, Grafana, Alertmanager, Node Exporter

### Nginx

Seul point d'entree utilisateur. Proxifie vers le Service interne `locatic:8080`.
Un sidecar `nginx-prometheus-exporter` expose `/metrics` sur le port `9113`.

### SQLite + volume

L'application utilise SQLite (`/data/agence.db`). Le chemin est monte depuis la
PVC Terraform `locatic-sqlite` (`ReadWriteOnce`, 1 replica).

### Monitoring

Prometheus scrape l'app (`/metrics`), Nginx (exporter), le noeud et le kubelet.
Grafana visualise l'etat des services. Alertmanager route les alertes vers un
webhook local de test.
