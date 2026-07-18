# Kubernetes

L'application Locatic est deployee sur un cluster Kubernetes local minikube.
Ce document decrit les ressources Kubernetes utilisees, les services exposes,
le stockage SQLite, le reverse proxy Nginx et la configuration de l'application.

## Vue d'ensemble

Le depot contient deux structures de manifests, complementaires :

- [`deploy/k8s/app/`](../deploy/k8s/app/) : manifests de l'application
  Locatic (Paul), avec overlays Kustomize pour `dev` et `prod`. Inclut
  `prometheus-net` pour exposer `/metrics`.
- [`infra/kubernetes/base/`](../infra/kubernetes/base/) : manifests de Nginx
  reverse proxy + une variante de l'application et de la PVC (Gires), prets a
  appliquer dans le namespace `locatic`.

```
deploy/k8s/
└── app/
    ├── base/                  # Manifests app (Kustomize)
    │   ├── kustomization.yaml
    │   ├── deployment.yaml
    │   ├── service.yaml
    │   ├── configmap.yaml
    │   └── secret.yaml.tpl    # Template - jamais de vrai secret commite
    └── overlays/
        ├── dev/               # Environnement staging (minikube dev)
        │   └── kustomization.yaml
        └── prod/              # Environnement prod local
            └── kustomization.yaml

infra/kubernetes/
└── base/
    ├── kustomization.yaml     # Regroupe app + nginx + pvc + configmap
    ├── app.yaml               # Deployment + Service app (variante Gires)
    ├── nginx.yaml             # Deployment + Service Nginx (reverse proxy)
    ├── configmap.yaml         # ConfigMap app + config Nginx
    └── pvc.yaml               # PVC SQLite
```

Les overlays Kustomize (`deploy/k8s/app/overlays/`) permettent de differencier
les environnements sans dupliquer les manifests :

- `namespace` : `locatic-staging` (dev) ou `locatic-prod` (prod) — cree par
  Terraform (voir `docs/terraform.md`).
- `images` : tag de l'image Docker deployee (`latest` en dev, SHA epingle en
  prod).
- `replicas` : 2 en dev, 3 en prod.

## Prerequis : Terraform

Les namespaces et la `PersistentVolumeClaim` SQLite sont geres par Terraform
(voir `docs/terraform.md`). Avant d'appliquer les manifests, il faut avoir
execute :

```bash
cd infra/terraform/environments/dev   # (ou prod)
terraform init && terraform apply
```

Cela cree :

- le namespace `locatic-staging` (ou `locatic-prod`) ;
- le namespace `monitoring` ;
- la `PersistentVolumeClaim` `locatic-sqlite` dans le namespace applicatif.

> Les manifests de Gires dans `infra/kubernetes/base/` ciblent le namespace
> `locatic`. Si vous utilisez cette variante, ajustez le namespace dans
> `terraform.tfvars` (`environment = ""` pour obtenir `locatic-`) ou editez les
> manifests. La variante `deploy/k8s/app/overlays/` est alignee avec les
> namespaces `locatic-staging`/`locatic-prod` de Terraform.

## Ressources Kubernetes de l'application

### Deployment (`deploy/k8s/app/base/deployment.yaml`)

- **Image** : `ghcr.io/pauldatcom/locatic:latest` (surchageable via overlay).
  `imagePullPolicy: IfNotPresent` — l'image est chargee dans minikube via
  `minikube image load` (voir `docs/deploiement-local.md`), pas tiree depuis
  ghcr.io a chaque demarrage.
- **Replicas** : 2 (dev) / 3 (prod).
- **SecurityContext** : `runAsNonRoot: true`, `runAsUser: 10001`,
  `fsGroup: 10001`. Le conteneur tourne en utilisateur non privilegie (le
  Dockerfile cree l'utilisateur `locatic`).
- **Probes** :
  - `livenessProbe` : `GET /health` (interval 20s).
  - `readinessProbe` : `GET /health` (interval 10s).
  - `startupProbe` : `GET /health` (30 essais x 5s = 150s pour demarrer).
- **Ressources** :
  - requests : `100m` CPU / `128Mi` memoire.
  - limits : `500m` CPU / `512Mi` memoire.
- **Volume** : `sqlite-data` monte sur `/data`, reference la PVC
  `locatic-sqlite` creee par Terraform. C'est la que SQLite ecrit
  `agence.db`.
- **Env** : chargees depuis `locatic-config` (ConfigMap) et `locatic-secret`
  (Secret). Les variables `POD_NAME` et `POD_NAMESPACE` sont injectees via
  `fieldRef` pour le logging.
- **Annotations Prometheus** : `prometheus.io/scrape: "true"`,
  `prometheus.io/port: "8080"`, `prometheus.io/path: "/metrics"`. Permettent a
  Prometheus de decouvrir automatiquement le pod et de scrapper `/metrics`.

### Service (`deploy/k8s/app/base/service.yaml`)

- **Type** : `ClusterIP` (pas expose directement — Nginx est le point d'entree).
- **Port** : `8080` (HTTP) sur le port nomme `http` du conteneur.
- Le Service n'est pas expose en `LoadBalancer` ou `NodePort` : seul Nginx
  (reverse proxy, ajoute par Gires) est expose.

### ConfigMap (`deploy/k8s/app/base/configmap.yaml`)

Variables d'environnement non sensibles :

| Variable | Valeur | Rôle |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Active la config de production ASP.NET. |
| `ASPNETCORE_URLS` | `http://+:8080` | Port d'ecoute de l'app. |
| `LOG_LEVEL` | `info` | Niveau de log (surchageable via overlay). |
| `SQLITE_PATH` | `/data/agence.db` | Chemin du fichier SQLite (monte sur PVC). |
| `ConnectionStrings__DefaultConnection` | `Data Source=/data/agence.db` | Chaine de connexion EF Core. |

Le `__` dans `ConnectionStrings__DefaultConnection` est la convention ASP.NET
pour surcharger une hierarchie `appsettings.json` via variable d'environnement.

### Secret (`deploy/k8s/app/base/secret.yaml.tpl`)

Template de Secret — ne contient **aucun secret reel**. L'application n'en
utilise pas actuellement (pas de base de donnees externe, pas d'API tierce).
Le template est fourni comme structure pour les ajouts futurs.

> Regle : aucun secret n'est jamais commite. Les placeholders (`CHANGE_ME`)
> sont surcharges par Ansible au deploiement (depuis un vault ou une saisie
> operateur). Voir `docs/ansible.md`.

## Reverse proxy Nginx (`infra/kubernetes/base/nginx.yaml`)

Ajoute par Gires. Le flux attendu :

```
Utilisateur -> Service locatic-nginx (NodePort) -> Pod Nginx -> Service locatic-app -> Pod Locatic -> PVC SQLite
```

- **Deployment** `locatic-nginx` : image `nginxinc/nginx-unprivileged:1.27-alpine`
  (non-root), 1 replica, probes `/health` via reverse proxy, requests/limits
  CPU/memoire, `securityContext` strict.
- **Service** `locatic-nginx` : `NodePort` sur minikube — c'est le point
  d'entree utilisateur.
- **ConfigMap** : configuration Nginx (`default.conf`) avec `proxy_pass` vers
  le Service `locatic-app:8080`.
- **Healthcheck** : Nginx expose `/health` en `proxy_pass` vers l'app, donc si
  l'app ne repond plus, l'entree Nginx devient non prete.

L'application ne doit jamais etre exposee directement. C'est une contrainte
forte du mini-projet (voir `docs/mini-project.md` §22).

## Stockage SQLite

- **PVC** : `locatic-sqlite` (creee par Terraform, voir `docs/terraform.md`).
  - Taille : `1Gi` (dev) / `2Gi` (prod).
  - Access mode : `ReadWriteOnce` (SQLite = un seul writer).
  - Reclaim policy : `Retain` (les donnees survivent a la suppression de la PVC).
- **Montage** : `/data` dans le conteneur. Le fichier `agence.db` y est ecrit
  par Entity Framework Core.
- **Persistance** : si le pod redemarre, le volume est re-monte et les donnees
  SQLite sont intactes. Verifie par la procedure dans
  `docs/exploitation.md`.

> La StorageClass par defaut de minikube (`standard`) provisionne
> dynamiquement la PV. Aucune PV hostPath n'est necessaire sauf si
> `sqlite_host_path` est renseigne dans `terraform.tfvars`.

## Configuration via overlays

### dev (staging)

`deploy/k8s/app/overlays/dev/kustomization.yaml` :

- `namespace: locatic-staging`
- `images`: `ghcr.io/pauldatcom/locatic:latest`
- `replicas`: 2

### prod

`deploy/k8s/app/overlays/prod/kustomization.yaml` :

- `namespace: locatic-prod`
- `images`: `ghcr.io/pauldatcom/locatic:<sha>` (epingle via
  `kustomize edit set image` ou par Ansible)
- `replicas`: 3

## Application des manifests

Les manifests sont appliques par Ansible (voir `docs/ansible.md`), qui lit les
outputs de Terraform pour valoriser le namespace et le tag d'image.

Pour appliquer manuellement (apres `terraform apply`) :

```bash
# Variante overlays (Paul) - app seule
kubectl apply -k deploy/k8s/app/overlays/dev

# Variante base regroupee (Gires) - app + nginx + pvc + configmap
kubectl apply -k infra/kubernetes/base
```

## Verification

```bash
# Pods applicatifs
kubectl get pods -n locatic-staging -l app.kubernetes.io/name=locatic

# Service
kubectl get svc -n locatic-staging locatic

# PVC utilisee
kubectl get pvc -n locatic-staging locatic-sqlite

# Nginx (si deploye via infra/kubernetes/base)
kubectl get deploy locatic-nginx -n locatic
kubectl get svc    locatic-nginx -n locatic
minikube service   locatic-nginx -n locatic --url

# Endpoint de sante
kubectl exec -n locatic-staging deploy/locatic -- curl -s http://localhost:8080/health

# Endpoint de metriques
kubectl exec -n locatic-staging deploy/locatic -- curl -s http://localhost:8080/metrics | head
```

Pour verifier que l'application passe bien par Nginx :

```bash
kubectl port-forward svc/locatic-nginx 8080:80 -n locatic
curl http://localhost:8080/health
```

Le endpoint `/metrics` doit renvoyer des lignes au format Prometheus
(`http_requests_total`, `http_requests_in_progress`, `process_*`...). C'est
ces metriques que Prometheus scrape (voir `docs/monitoring.md`).

## Metriques exposees

L'application expose `/metrics` grace au package `prometheus-net.AspNetCore`
(ajoute dans `Locatic/Locatic.csproj`). Les metriques principales :

- `http_requests_total` : nombre total de requetes HTTP (par code, methode).
- `http_requests_in_progress` : requetes en cours.
- `http_request_duration_seconds` : histogramme des temps de reponse.
- `process_*` : metriques processus (CPU, memoire, GC).
- `dotnet_*` : metriques runtime .NET.

Le middleware `app.UseHttpMetrics()` intercepte toutes les requetes HTTP et
`app.MapMetrics("/metrics")` expose le endpoint. Voir `Locatic/Program.cs`.

## Monitoring

Les manifests ajoutent des annotations Prometheus sur le service et le pod
applicatif pour preparer la collecte de `/metrics`. La partie monitoring
Prometheus/Grafana reste separee (voir `docs/monitoring.md`) afin que le
playbook Ansible puisse orchestrer l'ensemble du deploiement local.