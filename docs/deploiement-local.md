# Déploiement — Locatic

Ce document décrit la chaîne complète de déploiement de l'application **Locatic** sur un cluster Kubernetes local (**kind**), en combinant **Terraform** (infrastructure de base) et **Ansible** (build & déploiement applicatif).

## Vue d'ensemble

```
┌─────────────┐      ┌──────────────────┐      ┌─────────────────────┐
│  Terraform  │ ───▶ │  Cluster kind     │ ◀─── │      Ansible        │
│             │      │  (Kubernetes)     │      │                     │
│ - namespace │      │                   │      │ - docker build      │
│ - PVC       │      │  locatic-staging  │      │ - kind load image   │
│   sqlite    │      │  namespace        │      │ - kubectl apply     │
│ - namespace │      │                   │      │   (app + nginx)     │
│   monitoring│      │                   │      │                     │
└─────────────┘      └──────────────────┘      └─────────────────────┘
```

- **Terraform** crée les ressources qui doivent exister durablement et changer rarement : namespaces, PersistentVolumeClaim.
- **Ansible** orchestre le cycle de build/déploiement applicatif : construction de l'image Docker, chargement dans le cluster, application des manifests Kubernetes.

## Prérequis généraux

- **Docker Desktop** avec intégration WSL activée
- **kind** (Kubernetes in Docker) avec un cluster actif
- **kubectl** configuré et pointant vers le bon contexte
- **Terraform** ≥ 1.x
- **Ansible** avec la collection `kubernetes.core`
- **.NET SDK 8.0** (pour dev/tests locaux hors conteneur)

Vérifications rapides :
```bash
docker --version
kind get clusters
kubectl config current-context
terraform --version
ansible --version
```

## Étape 1 — Infrastructure de base (Terraform)

Dossier : `infra/terraform/environments/dev`

### Variables clés (`terraform.tfvars`)

| Variable | Description | Exemple |
|---|---|---|
| `kubeconfig_path` | Chemin vers le fichier kubeconfig | `~/.kube/config` |
| `kube_context` | Contexte kubectl à utiliser | `kind-kind` |
| `monitoring_namespace` | Namespace dédié au monitoring | `monitoring` |

⚠️ `kube_context` doit correspondre exactement à un contexte existant. Vérifier avec :
```bash
kubectl config get-contexts
```

### Commandes

```bash
cd infra/terraform/environments/dev
terraform init
terraform plan
terraform apply
```

### Ressources créées

- Namespace applicatif : `locatic-staging`
- Namespace monitoring : `monitoring`
- PersistentVolumeClaim `locatic-sqlite` (namespace `locatic-staging`), storage class `standard` (mode `WaitForFirstConsumer` — reste `Pending` tant qu'aucun pod ne le monte, c'est normal)

### Vérification

```bash
kubectl get namespaces
kubectl get pvc -n locatic-staging
```

## Étape 2 — Build & déploiement applicatif (Ansible)

Dossier : `infra/ansible`

Voir le [README dédié](./ansible/README.md) pour le détail des tâches, variables et rôles.

### Commandes

```bash
cd infra/ansible
ansible-galaxy collection install -r requirements.yml
pip install kubernetes
ansible-playbook -i inventory.yml deploy-k8s.yml
```

### Ce que ça déploie

- **locatic-app** : conteneur de l'application .NET (image buildée localement depuis le `Dockerfile` à la racine du repo), montant le PVC `locatic-sqlite` sur `/data`
- **locatic-nginx** : reverse-proxy devant l'application, exposé en `NodePort`

### Vérification

```bash
kubectl get all -n locatic-staging
kubectl get pvc -n locatic-staging   # doit passer à Bound
```

## Étape 3 — Accès à l'application

Le cluster kind local n'expose pas les NodePort directement sur `localhost`. Utiliser un port-forward :

```bash
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
```

Puis :
```bash
curl http://localhost:8888/health
```
ou dans un navigateur : `http://localhost:8888`

## Étape 4 — Monitoring (à venir)

Le namespace `monitoring` est déjà provisionné par Terraform. Les manifests de `locatic-app` et `locatic-nginx` sont déjà annotés pour Prometheus (`prometheus.io/scrape: "true"`, `prometheus.io/path: "/metrics"`, `prometheus.io/port: "8080"`), prêts pour le scraping une fois la stack Prometheus/Grafana déployée.

## Ordre complet, de zéro à l'app accessible

```bash
# 1. Infra de base
cd infra/terraform/environments/dev
terraform init && terraform apply

# 2. Build + déploiement applicatif
cd ../../../ansible
ansible-galaxy collection install -r requirements.yml
ansible-playbook -i inventory.yml deploy-k8s.yml

# 3. Accès
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
```

## Dépannage général

| Symptôme | Solution |
|---|---|
| `context "X" does not exist` (Terraform) | Corriger `kube_context` dans `terraform.tfvars` avec `kubectl config get-contexts` |
| `dial tcp ... connection refused` | Le cluster kind ou Docker Desktop est arrêté — vérifier `docker ps` et relancer Docker Desktop |
| `docker: command not found` dans WSL | Réactiver l'intégration WSL dans Docker Desktop (Settings → Resources → WSL Integration) |
| PVC bloqué en `Pending` indéfiniment sans pod qui le monte | Comportement normal avec `WaitForFirstConsumer` — se résout une fois l'app déployée |
| Build Docker tué en cours de route | Manque de RAM WSL — configurer `.wslconfig` côté Windows et relancer `wsl --shutdown` |