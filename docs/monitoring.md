# Monitoring (Prometheus / Grafana / Alertmanager)

Ce document decrit la stack de monitoring deployee sur le cluster minikube pour
superviser l'application Locatic, le reverse proxy Nginx, le noeud Kubernetes
et les composants de la stack elle-meme. Spec steps **17** et **18** de
`docs/mini-project.md`.

## Vue d'ensemble

La stack de monitoring est deployee dans le namespace **`monitoring`** (cree
par Terraform, voir `docs/terraform.md`). Elle est constituee de :

| Composant       | Rôle | Image |
| --- | --- | --- |
| **Prometheus**    | Collecte et stocke les metriques (TSDB) | `prom/prometheus:v3.9.1` |
| **Grafana**       | Visualisation (dashboards) | `grafana/grafana:12.3.1` |
| **Alertmanager**  | Routage des alertes vers un webhook | `prom/alertmanager:v0.30.0` |
| **Node Exporter** | Metriques du noeud minikube (CPU, mem, disque) | `prom/node-exporter:v1.10.2` |
| **webhook-mock**  | Endpoint HTTP qui logge les alertes (dev) | `node:20-alpine` |

Structure des manifests :

```
deploy/k8s/monitoring/
├── base/
│   ├── kustomization.yaml
│   ├── prometheus-rbac.yaml          # ServiceAccount + ClusterRole/Binding
│   ├── prometheus-config.yaml        # ConfigMap : prometheus.yml + alerts.yml
│   ├── prometheus.yaml               # PVC + Deployment + Service
│   ├── alertmanager.yaml             # ConfigMap + Deployment + Service
│   ├── webhook-mock.yaml             # Deployment + Service (recoit les alertes)
│   ├── grafana-provisioning.yaml     # ConfigMap : datasources + dashboards provider
│   ├── grafana-dashboard.yaml        # ConfigMap : dashboard JSON "Locatic - Vue d'ensemble"
│   ├── grafana-secret.yaml.tpl       # Template Secret (cree par Ansible)
│   ├── grafana.yaml                  # PVC + Deployment + Service
│   └── node-exporter.yaml            # DaemonSet + Service (metriques noeud)
└── overlays/
    ├── dev/kustomization.yaml        # namespace=monitoring, ressources dev
    └── prod/kustomization.yaml       # namespace=monitoring, ressources prod
```

## Services monitorises

Prometheus scrape les cibles suivantes (voir `prometheus-config.yaml`):

| Job           | Cible | Endpoint | Rôle |
| --- | --- | --- | --- |
| `prometheus`  | `localhost:9090` | `/metrics` | Auto-supervision Prometheus |
| `locatic-app` | pods Locatic (decouverte K8s) | `/metrics` | Metriques HTTP de l'app (prometheus-net) |
| `nginx`       | pods Nginx + sidecar exporter (port 9113) | `/metrics` | Metriques Nginx (nginx-prometheus-exporter) |
| `node`        | `node-exporter:9100` | `/metrics` | CPU, memoire, disque du noeud |
| `kubelet`     | API Kubelet (https) | `/metrics` | Metriques internes Kubernetes |

### Decouverte Kubernetes pour l'app Locatic

Le job `locatic-app` utilise `kubernetes_sd_configs` (role `pod`) pour
decouvrir automatiquement les pods Locatic dans le namespace
`locatic-staging`. Les relabel_configs filtrent :

- `app.kubernetes.io/name = locatic`
- annotation `prometheus.io/scrape = "true"`
- annotation `prometheus.io/path = "/metrics"`
- annotation `prometheus.io/port` (definit le port scrape)

Ce mecanisme s'appuie sur les annotations posees sur le pod Locatic par le
Deployment `deploy/k8s/app/base/deployment.yaml` (PR #6). RBAC Prometheus
(`prometheus-rbac.yaml`) accorde les droits `get/list/watch` sur pods,
services, endpoints et namespaces.

### Metriques de l'application Locatic

L'application expose `/metrics` via le package `prometheus-net.AspNetCore`
(ajoute dans `Locatic/Locatic.csproj`, PR #6). Metriques principales :

- `http_requests_total` : nombre total de requetes HTTP (par code, methode).
- `http_requests_in_progress` : requetes en cours.
- `http_request_duration_seconds` : histogramme des temps de reponse.
- `process_*` : metriques processus (CPU, memoire, GC).
- `dotnet_*` : metriques runtime .NET.

## Alertes

Les regles d'alerte sont definies dans `prometheus-config.yaml` (cle
`rules/alerts.yml`) et chargees par Prometheus via `rule_files`. Detail des
alertes :

### App alerts (`app-alerts`)

| Alert | Expression | Severity | Description |
| --- | --- | --- | --- |
| `AppDown` | `up{job="locatic-app"} == 0` pendant 1m | critical | L'app ne repond plus |
| `HighLatency` | `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="locatic-app"}[5m])) > 0.5` pendant 2m | warning | P95 > 500ms |
| `HighErrorRate` | `sum(rate(http_requests_total{job="locatic-app",status=~"5.."}[5m])) / sum(rate(http_requests_total{job="locatic-app"}[5m])) > 0.05` pendant 2m | warning | > 5% de 5xx |

### Infra alerts (`infra-alerts`)

| Alert | Expression | Severity | Description |
| --- | --- | --- | --- |
| `HighCPU` | CPU idle < 20% pendant 5m | warning | CPU > 80% |
| `DiskAlmostFull` | Disque libre < 15% pendant 5m | warning | Espace disque critique |
| `PrometheusTargetDown` | Plus d'1 cible down pendant 2m | warning | Cibles injoignables |

### Routage des alertes

Alertmanager (`alertmanager-config.yaml`) route les alertes :

- `severity=critical` -> receiver `critical` (webhook `webhook-mock:5001`)
- autres -> receiver `default` (webhook `webhook-mock:5001`)

Le `webhook-mock` est un simple serveur HTTP Node qui logge les payloads
recus. En prod, on remplace l'URL par Slack / PagerDuty / email.

## Grafana

### Provisionning automatique

Grafana est provisionne automatiquement au demarrage via deux ConfigMaps :

- `grafana-provisioning` :
  - `datasources.yml` : declare Prometheus (`http://prometheus:9090`) comme
    datasource par defaut.
  - `dashboards.yml` : declare un provider de dashboards qui lit les fichiers
    JSON dans `/var/lib/grafana/dashboards`.
- `grafana-dashboards` : contient le dashboard `app-overview.json`.

Aucune configuration manuelle n'est necessaire : au demarrage, Grafana se
connecte a Prometheus et charge le dashboard.

### Dashboard "Locatic - Vue d'ensemble"

Le dashboard `app-overview.json` (dans `grafana-dashboard.yaml`) contient les
panels suivants :

| Panel | Type | Requête Prometheus |
| --- | --- | --- |
| Cibles Prometheus UP | stat | `count(up == 1)` |
| Cibles DOWN | stat | `count(up == 0)` |
| Pods Locatic prets | stat | `count(up{job="locatic-app"} == 1)` |
| Erreurs 5xx (rate) | stat | `sum(rate(http_requests_total{job="locatic-app",status=~"5.."}[5m]))` |
| Taux de requetes HTTP | timeseries | `rate(http_requests_total{job="locatic-app"}[5m])` |
| Latence P95 / P99 | timeseries | `histogram_quantile(...)` |
| CPU du noeud minikube | timeseries | `100 - (avg(rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)` |
| Memoire utilisee | timeseries | `(node_memory_MemTotal - MemAvailable) / 1024 / 1024` |
| Espace disque disponible | timeseries | `(node_filesystem_avail / size) * 100` |
| Etat des cibles Prometheus | table | `up` (instant) |

Le dashboard permet de comprendre rapidement :

- si l'application Locatic repond (pod UP, metriques HTTP presentes) ;
- si le noeud minikube est sature (CPU, memoire, disque) ;
- si la persistance SQLite est menacée (disque presque plein) ;
- si Nginx et le monitoring lui-meme fonctionnent (cibles UP).

### Credentials Grafana

Le Secret `grafana-admin-secret` **n'est pas versionne**. Ansible le cree au
deploiement (`infra/ansible/roles/k8s_deploy`). Defaults locaux dans
`roles/k8s_deploy/defaults/main.yml` (surchargeables via `-e`).

> Ne jamais committer de vrai mot de passe. Utiliser `grafana-secret.yaml.tpl`
> uniquement comme modele.

## Acces aux interfaces

Prometheus, Grafana et Alertmanager exposent des Services `ClusterIP` dans le
namespace `monitoring`. Pour y acceder depuis votre poste, utiliser
`kubectl port-forward`.

### Prometheus

```bash
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# -> http://localhost:9090
```

Verifier les cibles scrapees : `http://localhost:9090/targets`

### Grafana

```bash
kubectl port-forward -n monitoring svc/grafana 3000:3000
# -> http://localhost:3000
# Login : admin / devops-training-local (defaut)
```

Le dashboard "Locatic - Vue d'ensemble" est dans le dossier "DevOps Locatic".

### Alertmanager

```bash
kubectl port-forward -n monitoring svc/alertmanager 9093:9093
# -> http://localhost:9093
```

Verifier les alertes actives : `http://localhost:9093/#/alerts`

### webhook-mock (logs des alertes)

```bash
kubectl logs -n monitoring deploy/webhook-mock -f
# Affiche les payloads d'alerte recus en temps reel.
```

## Application des manifests

La stack est appliquee par Ansible (voir `docs/ansible.md`), qui lit les
outputs Terraform pour valoriser le namespace.

Pour appliquer manuellement (apres `terraform apply`) :

```bash
# Dev
kubectl apply -k deploy/k8s/monitoring/overlays/dev

# Prod
kubectl apply -k deploy/k8s/monitoring/overlays/prod
```

> Le RBAC Prometheus (ServiceAccount, ClusterRole, ClusterRoleBinding) est
> cluster-scoped : il s'applique independamment du namespace et donne a
> Prometheus les droits de decouverte Kubernetes necessaires.

## Verification

```bash
# Pods de monitoring
kubectl get pods -n monitoring

# Services
kubectl get svc -n monitoring

# PVC Prometheus et Grafana (Bound)
kubectl get pvc -n monitoring

# Cibles Prometheus UP
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Ouvrir http://localhost:9090/targets : tous les jobs doivent etre UP,
# sauf 'nginx' tant qu'un exporteur Nginx /metrics n'est pas ajoute.

# Dashboard Grafana
kubectl port-forward -n monitoring svc/grafana 3000:3000
# Ouvrir http://localhost:3000, dossier "DevOps Locatic", dashboard
# "Locatic - Vue d'ensemble".
```

Le script `scripts/verify.sh` (PR #8) verifie automatiquement que les targets
Prometheus sont UP.

## Limites connues

- **Metriques Nginx** : un sidecar `nginx-prometheus-exporter` scrape
  `stub_status` (`/nginx_status`) et expose `/metrics` sur le port `9113`.
  Prometheus decouvre ce sidecar via `kubernetes_sd_configs`.
- **Namespace applicatif en prod** : le ConfigMap `prometheus-config` contient
  en dur `locatic-staging` dans le job `locatic-app`. Pour pointer vers
  `locatic-prod`, soit templatiser le ConfigMap via Ansible, soit utiliser un
  second overlay avec une ConfigMap dediee. En pratique, le job
  `kubernetes_sd_configs` filtre par `namespaces.names`, qu'on peut surcharger
  via un patch Kustomize dedie si besoin.
- **Alertmanager en single-instance** : suffisant pour le developpement
  local, non tolerent aux pannes. En prod, deployer 3 replicas + un
  PeerSet.
- **Retention TSDB** : 7 jours (configurable dans `prometheus.yaml` via
  `--storage.tsdb.retention.time`).
- **Pas d'authentification Prometheus** : Prometheus est expose uniquement en
  ClusterIP, accessible via port-forward. En prod, ajouter un reverse proxy
  authentifie (Grafana peut servir de frontend via son datasource proxy).

## Relation avec docker-compose.yml

Le depot contient aussi un `docker-compose.yml` a la racine, qui deploye la
meme stack Prometheus/Grafana/Alertmanager/node-exporter/webhook-mock en
conteneurs Docker (sans Kubernetes). Ce compose sert au developpement local de
la configuration Prometheus/Grafana hors minikube. Les fichiers de config
sources sont dans `monitoring/` (partages entre le compose et les ConfigMaps
Kubernetes, meme contenu).

Sur minikube, **on utilise les manifests Kubernetes** (`deploy/k8s/monitoring/`),
pas le compose. Le compose est une alternative hors-cluster.