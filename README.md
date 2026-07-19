# Locatic — DevOps

Application ASP.NET Core MVC de gestion d'une agence de location de voitures
(SQLite), avec une chaîne DevOps complète : CI GitHub Actions, image Docker,
Terraform, Ansible, Kubernetes (minikube) derrière Nginx, Prometheus / Grafana.

Base POO : [Projet_Locatic](https://github.com/Louange-03/Projet_Locatic)  
Consigne : [`docs/mini-project.md`](docs/mini-project.md)

---

## Démarrage rapide (chemin recommandé)

Déploie **tout** sur minikube : Terraform → build image → app + Nginx + monitoring.

### Windows (Docker Desktop)

```powershell
git clone https://github.com/Pauldatcom/DevOps-Locatic-Hetic.git
cd DevOps-Locatic-Hetic

.\scripts\setup-prereqs.ps1   # une fois : minikube, Terraform, outils
.\scripts\deploy.ps1          # déploiement complet
.\scripts\verify.ps1          # health, /metrics, cibles Prometheus
```

### Linux / macOS

```bash
git clone https://github.com/Pauldatcom/DevOps-Locatic-Hetic.git
cd DevOps-Locatic-Hetic

./scripts/setup-prereqs.sh    # une fois si besoin
./scripts/deploy.sh dev minikube
./scripts/verify.sh
```

### Accès après déploiement

```powershell
# Application (via Nginx — point d'entrée)
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
# → http://127.0.0.1:8888
# → http://127.0.0.1:8888/health
# → http://127.0.0.1:8888/metrics

# Prometheus
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# → http://127.0.0.1:9090/targets

# Grafana
kubectl port-forward -n monitoring svc/grafana 3000:3000
# → http://127.0.0.1:3000  (admin / devops-training-local)
```

Sur Windows, préférer `deploy.ps1` (évite les soucis WSL / kubeconfig).  
Détail : [`docs/deploiement-local.md`](docs/deploiement-local.md).

---

## Prérequis

| Outil | Usage |
| --- | --- |
| Docker Desktop (ou Engine) | Build image + driver minikube |
| minikube | Cluster Kubernetes local |
| kubectl | Appliquer / vérifier les ressources |
| Terraform >= 1.6 | Namespaces + PVC SQLite |
| Ansible (+ `kubernetes.core`) | Orchestration Linux (`deploy.sh`) |
| .NET 8 SDK | Lancement app hors Docker (optionnel) |
| `dotnet-ef` | Migrations SQLite hors Docker (optionnel) |

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

---

## Architecture (vue simple)

```txt
GitHub (PR + CI)
  → build / tests / scan
  → push image ghcr.io/pauldatcom/locatic (sur main uniquement)

Machine locale
  → Terraform : namespaces + PVC SQLite
  → Ansible / deploy.ps1 : image + manifests
  → minikube
       Utilisateur → Nginx (NodePort) → App Locatic (:8080)
                                       → SQLite sur PVC /data
       Monitoring  → Prometheus + Grafana (+ Alertmanager, Node Exporter)
```

Le pipeline GitHub **ne déploie pas** sur minikube. Le déploiement se fait en local.

---

## Structure du dépôt

```txt
.
├── .github/workflows/     # CI (build, tests, Trivy, publish GHCR)
├── Locatic/               # Application ASP.NET Core MVC + tests xUnit
├── Dockerfile             # Image runtime (USER 999, volume /data)
├── docker-compose.yml     # Monitoring hors cluster (optionnel / dev rapide)
├── deploy/k8s/            # Manifests Kustomize : app, nginx, monitoring
├── infra/
│   ├── terraform/         # Namespaces + PVC (env dev / prod)
│   └── ansible/           # Playbook deploy-k8s.yml (role k8s_deploy)
├── monitoring/            # Config Prometheus/Grafana pour docker-compose
├── scripts/               # setup / deploy / verify (Windows + Linux)
└── docs/                  # Documentation détaillée + preuves
```

---

## Autres façons de lancer l'application

### A. .NET local (sans Docker / sans Kubernetes)

```bash
cd Locatic
dotnet restore
dotnet ef database update
dotnet run
```

URL typique : `http://localhost:5286`

### B. Docker seul (sans Kubernetes)

```bash
docker build -t locatic:latest .
docker run -p 8080:8080 -v locatic-data:/data locatic:latest
```

→ `http://localhost:8080` — SQLite persisté dans le volume `locatic-data`.

### C. Monitoring hors cluster (docker-compose)

```bash
docker compose up -d
```

Pour le mini-projet, le monitoring **cible** est celui déployé dans Kubernetes
(`deploy/k8s/monitoring`). Voir [`docs/monitoring.md`](docs/monitoring.md).

---

## Fonctionnalités métier

- Marques, modèles, voitures (CRUD), clients, réservations
- Persistance SQLite (EF Core) + seed au démarrage
- Architecture MVC + services + injection de dépendances

Relations : Brand → Modele → Car ; Client / Car → Reservation.

---

## CI / GitHub

Workflow : `.github/workflows/ci.yml`

| Sur Pull Request | Sur `main` |
| --- | --- |
| lint, build, tests | idem + |
| scan Trivy | **publication** image `ghcr.io/pauldatcom/locatic` |

Règles attendues :

- pas de push direct sur `main`
- merge via Pull Request + checks CI verts
- secrets et `terraform.tfstate` **jamais** versionnés

Détail : [`docs/ci-cd.md`](docs/ci-cd.md).

---

## Configuration utile

| Contexte | SQLite |
| --- | --- |
| Dev local (.NET) | `Data Source=agence.db` (`appsettings`) |
| Conteneur / K8s | `Data Source=/data/agence.db` (env + PVC) |

Endpoints applicatifs :

- `/health` — readiness / liveness Kubernetes
- `/metrics` — scrape Prometheus (`prometheus-net`)

---

## Commandes utiles

```bash
# Tests
dotnet test Locatic/Locatic.csproj

# Migrations (hors Docker)
dotnet ef migrations add NomMigration --project Locatic
dotnet ef database update --project Locatic

# Cluster
kubectl get all,pvc -n locatic-staging
kubectl get all -n monitoring
kubectl logs -n locatic-staging deploy/locatic

# Persistance SQLite (après delete du pod app)
kubectl delete pod -n locatic-staging -l app.kubernetes.io/component=app
kubectl exec -n locatic-staging deploy/locatic -- ls -la /data/agence.db
```

---

## Documentation

| Document | Contenu |
| --- | --- |
| [`docs/mini-project.md`](docs/mini-project.md) | Consigne complète |
| [`docs/architecture.md`](docs/architecture.md) | Architecture globale |
| [`docs/deploiement-local.md`](docs/deploiement-local.md) | Déploiement minikube pas à pas |
| [`docs/ci-cd.md`](docs/ci-cd.md) | Pipeline et règles de branche |
| [`docs/terraform.md`](docs/terraform.md) | Infra locale |
| [`docs/ansible.md`](docs/ansible.md) | Orchestration |
| [`docs/kubernetes.md`](docs/kubernetes.md) | Manifests app / Nginx |
| [`docs/monitoring.md`](docs/monitoring.md) | Prometheus / Grafana / alertes |
| [`docs/exploitation.md`](docs/exploitation.md) | Vérifs, logs, rollback |
| [`docs/helm.md`](docs/helm.md) | Bonus Helm (non réalisé) |
| [`docs/preuves/`](docs/preuves/) | Captures et preuves de rendu |

---

## Équipe

- Esso Mawaki ASSIAH
- Gires TIENTCHEU
- Paul COMPAGNON

Dépôt : https://github.com/Pauldatcom/DevOps-Locatic-Hetic
