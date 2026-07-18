# Architecture

Ce document explique l'architecture globale de la chaine DevOps mise en place
autour du projet Locatic. Le projet de depart (POO) est une application
**ASP.NET Core MVC** de gestion d'une agence de location de voitures, avec
persistance **SQLite** via Entity Framework Core. La couche DevOps ajoute une
chaine complete : GitHub, CI/CD, conteneurisation, infrastructure as code,
orchestration, deploiement Kubernetes local et monitoring.

Le schéma officiel de la consigne est dans
[`docs/assets/mini-project-architecture.png`](assets/mini-project-architecture.png).

## Vue d'ensemble

```
                        +-----------------------+
                        |   GitHub (depot)      |
                        |  - code applicatif    |
                        |  - Pull Requests      |
                        |  - branch main/dev    |
                        |    protegees          |
                        +-----------+-----------+
                                    |
                                    v
                        +-----------------------+
                        |  GitHub Actions (CI)  |
                        |  - lint / build / test|
                        |  - scan Trivy         |
                        |  - build image Docker |
                        |  - publish -> ghcr.io |
                        +-----------+-----------+
                                    |
                          (le pipeline s'arrete ici)
                                    |
                                    v
                        +-----------------------+
                        |  Machine operateur    |
                        |  (Paul / Gires)      |
                        |  - minikube local     |
                        |  - terraform apply    |
                        |  - ansible-playbook   |
                        +-----------+-----------+
                                    |
                       +------------+------------+
                       |                         |
                       v                         v
            +------------------+      +--------------------+
            |  Terraform       |      |  Ansible           |
            |  - namespaces    | -->  |  - lit tf output   |
            |  - PVC SQLite    |      |  - applique K8s    |
            |  - outputs JSON  |      |    + Nginx + monit. |
            +------------------+      +--------------------+
                                                 |
                                                 v
                                      +---------------------+
                                      |  minikube (K8s)     |
                                      |                     |
                                      |  Namespace: locatic |
                                      |  - Nginx (entry)    |
                                      |  - Locatic app      |
                                      |  - PVC SQLite /data |
                                      |                     |
                                      |  Namespace: monitor |
                                      |  - Prometheus       |
                                      |  - Grafana          |
                                      |  - Alertmanager     |
                                      +---------------------+
```

## Roles des composants

### GitHub

- **Depot central** : `https://github.com/Pauldatcom/DevOps-Locatic-Hetic`.
- Le projet POO Locatic est repris comme base (branche d'origine :
  `Louange-03/Projet_Locatic`).
- Deux branches longues : `develop` (integration, cible des PRs) et `main`
  (stable, resultante des merges de `develop`).
- Toute modification passe par une **Pull Request** : pas de push direct sur
  `main` ni `develop`.

### GitHub Actions (CI)

- Workflow : [`.github/workflows/ci.yml`](../.github/workflows/ci.yml).
- Declencheur : `pull_request` et `push` sur `main`/`develop`.
- Jobs (en chaine) : `lint` -> `build` (compile + tests xUnit) -> `security`
  (Trivy scan du code) -> `docker-build` (workflow reutilisable) -> `publish`
  (uniquement sur `main`, publie l'image sur `ghcr.io`).
- **Le pipeline s'arrete volontairement apres la publication de l'image** :
  il ne deploie pas sur minikube. Raison : minikube tourne sur la machine de
  l'operateur, pas sur les runners GitHub. Le deploiement final est local.
- Detail dans [`docs/ci-cd.md`](ci-cd.md).

### Docker

- `Dockerfile` multi-stage (build SDK -> runtime aspnet:8.0).
- Utilisateur non-root (`locatic`), volume `/data` pour SQLite, HEALTHCHECK
  sur `/health`.
- Image publiee : `ghcr.io/pauldatcom/locatic:latest` + tag SHA sur `main`.
- Chargee dans minikube via `minikube image load` (pas de pull depuis ghcr.io
  a chaque demarrage, pas d'imagePullSecrets necessaire). Voir
  [`docs/deploiement-local.md`](deploiement-local.md).

### Terraform (infrastructure locale)

- Dossier : [`infra/terraform/`](../infra/terraform/).
- Provider **kubernetes** cible le contexte `minikube` du kubeconfig local.
- Cree :
  - le namespace applicatif `locatic-${environment}` (ex. `locatic-staging`,
    `locatic-prod`) ;
  - le namespace `monitoring` ;
  - la `PersistentVolumeClaim` `locatic-sqlite` montee sur `/data` pour
    SQLite (StorageClass `standard` de minikube, reclaim `Retain`).
- Produit des outputs JSON (`ansible_vars`) consommes par Ansible.
- L'etat `terraform.tfstate` reste local, jamais commite.
- Detail dans [`docs/terraform.md`](terraform.md).

### Ansible (orchestration locale)

- Dossier : `infra/ansible/` (apporte par Gires).
- Lit `terraform output -json` pour valoriser les manifests (namespace, PVC,
  image, tag).
- Verifie les prerequis (minikube, kubectl, helm installes et cluster actif).
- Applique les manifests Kubernetes : application Locatic, Nginx reverse
  proxy, stack de monitoring (Prometheus / Grafana / Alertmanager).
- Detail dans [`docs/ansible.md`](ansible.md).

### Kubernetes (minikube)

- Cluster local single-node demarre par `minikube start`.
- Namespaces :
  - `locatic-staging` / `locatic-prod` : application + Nginx.
  - `monitoring` : Prometheus, Grafana, Alertmanager.
- Ressources applicatives : `deploy/k8s/app/` (Deployment, Service ClusterIP,
  ConfigMap, Secret template) + `deploy/k8s/nginx/` (Gires) pour le reverse
  proxy.
- Le point d'entree utilisateur est **Nginx** (Service expose), pas
  l'application. L'application n'est joignable que depuis l'interieur du
  cluster.
- Detail dans [`docs/kubernetes.md`](kubernetes.md).

### Nginx (reverse proxy)

- Apporte par Gires dans `deploy/k8s/nginx/`.
- Deployment + ConfigMap (directive `proxy_pass` vers le Service `locatic`
  sur le port 8080) + Service expose (NodePort ou LoadBalancer minikube).
- C'est le **seul** point d'entree utilisateur : respect de la contrainte du
  mini-projet (l'application ne doit pas etre exposee directement).

### SQLite (persistance)

- Aucune base de donnees externe : SQLite est un fichier `agence.db` ecrit
  par EF Core dans le conteneur.
- Le fichier est stocke sur la PVC `locatic-sqlite` montee sur `/data`.
- La PVC est provisionnee par Terraform (StorageClass standard de minikube),
  reclaim policy `Retain` : les donnees survivent a la suppression du pod et
  meme de la PVC.
- Verifie par la procedure de redemarrage de pod dans
  [`docs/exploitation.md`](exploitation.md).

### Monitoring (Prometheus / Grafana / Alertmanager)

- Apporte par Gires dans `deploy/k8s/monitoring/` + `monitoring/`.
- **Prometheus** : scrape les metriques de l'application (`/metrics` expose
  par `prometheus-net`), de Nginx, et des composants Kubernetes (kubelet,
  node-exporter).
- **Grafana** : dashboard `app-overview.json` provisionne automatiquement,
  affiche l'etat de chaque service (app, Nginx, stockage, monitoring).
- **Alertmanager** : alertes simples (pod down, CPU eleve, PVC pleine)
  definies dans `monitoring/prometheus/alerts.yml`.
- Detail dans [`docs/monitoring.md`](monitoring.md).

## Flux de deploiement complet

1. Un developpeur ouvre une Pull Request vers `develop`.
2. GitHub Actions execute lint + build + tests + scan Trivy + build Docker.
3. La PR est revue et mergee dans `develop`.
4. Periodiquement (ou manuellement), `develop` est merge dans `main`.
5. Sur `main`, le job `publish` publie l'image `ghcr.io/pauldatcom/locatic:<sha>`
   et `:latest` sur ghcr.io.
6. Sur la machine operateur :
   - `minikube start` ;
   - `docker pull ghcr.io/pauldatcom/locatic:latest` ;
   - `minikube image load locatic:latest` ;
   - `cd infra/terraform/environments/dev && terraform apply` ;
   - `ansible-playbook infra/ansible/playbook.yml` (lit les outputs Terraform,
     applique les manifests K8s).
7. Nginx expose le Service Locatic ; l'utilisateur accede via Nginx.
8. Prometheus scrape `/metrics`, Grafana affiche les dashboards.

Le script [`scripts/deploy.sh`](../scripts/deploy.sh) (PR suivante) automatise
les etapes 6 et 7.

## Choix techniques principaux

- **Kubernetes local plutot que cloud** : la consigne impose minikube, aucun
  VPS. Tout est reproductible sur la machine de l'operateur.
- **Terraform pour l'infra, Ansible pour le deploiement** : separation claire
  entre ressources persistantes (namespaces, volumes) et ressources
  applicatives (Deployments, Services). Terraform prepare, Ansible deploie.
- **Kustomize pour les overlays** : pas de duplication de manifests entre dev
  et prod. Seuls le namespace, le tag d'image et le nombre de replicas
  different.
- **SQLite sur PVC** : pas de SGBD externe, un simple fichier. Le volume
  persistant garantit la survive des donnees.
- **Nginx comme unique point d'entree** : respect strict de la consigne.
  L'application n'est jamais exposee directement.
- **Prometheus-net pour les metriques app** : package leger, expose
  `/metrics` au format Prometheus sur le meme port que l'app, sans service
  supplementaire.

## Limites connues

- Tout est local : si la machine operateur est eteinte, rien ne tourne.
- L'etat Terraform n'est pas partage (backend local) : un seul operateur a la
  fois par environnement.
- `minikube image load` doit etre re-execute a chaque nouvelle image
  publiee (pas de pull automatique depuis ghcr.io).
- Le monitoring ne surveille qu'un cluster local ; pas d'alerting distant
  (Alertmanager envoie vers un webhook mock local).