# LOCATIC

Application web de gestion d'une agence de location de voitures developpee avec ASP.NET Core MVC et Entity Framework Core.

Ce depot reprend le projet de POO [Projet_Locatic](https://github.com/Louange-03/Projet_Locatic) comme base et y ajoute une chaine DevOps complete : Pull Requests avec branche `main` protegee, pipeline CI GitHub Actions, conteneurisation Docker, infrastructure Terraform, orchestration Ansible, deploiement Kubernetes sur minikube derriere Nginx, et supervision Prometheus/Grafana. Le detail de la demarche est dans [`docs/mini-project.md`](docs/mini-project.md).

## 1. Presentation du projet

Locatic permet de gerer les elements metier principaux d'une agence de location :
- Marques
- Modeles
- Voitures
- Clients
- Reservations

Le projet suit une architecture claire (MVC + Services) avec persistance SQLite.

## 2. Technologies utilisees

Application :
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core 8
- SQLite
- Razor Views
- Bootstrap 5

DevOps ajoute a l'application :
- Docker
- GitHub Actions
- Terraform
- Ansible
- Kubernetes (minikube)
- Prometheus / Grafana

## 3. Structure du depot

```
.
├── .github/workflows/   # Pipeline CI (lint, build, tests, scan Trivy, build/publish image)
├── Locatic/             # Code applicatif ASP.NET Core MVC + Tests xUnit
├── Dockerfile           # Image de l'application (multi-stage, non-root, /health)
├── docker-compose.yml   # Stack de monitoring locale (Prometheus/Grafana/Alertmanager)
├── deploy/k8s/app/      # Manifests Kubernetes de l'application (Kustomize + overlays dev/prod)
├── chart/locatic/       # Chart Helm bonus (alternative a Kustomize, rollback natif)
├── infra/
│   ├── terraform/       # Infrastructure locale minikube (namespaces, PVC SQLite)
│   └── ansible/         # Orchestration du deploiement local (Gires)
├── monitoring/          # Configuration Prometheus, Grafana, Alertmanager
└── docs/                # Documentation detaillee (voir §16)
```

## 4. Modele de donnees (relations)

- Brand 1 -> n Modele
- Modele 1 -> n Car
- Client 1 -> n Reservation
- Car 1 -> n Reservation

## 5. Prerequis

- SDK .NET 8 installe
- Outil EF Core CLI installe (dotnet-ef)
- Docker (pour la conteneurisation et le monitoring local)
- minikube, Terraform, Ansible (pour le deploiement local complet, voir `docs/deploiement-local.md`)

Installation de dotnet-ef (si necessaire) :

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

## 6. Installation et lancement en local (sans Docker)

1. Cloner le depot :

```bash
git clone git@github.com:Pauldatcom/DevOps-Locatic-Hetic.git
```

2. Se placer dans le projet web :

```bash
cd DevOps-Locatic-Hetic/Locatic
```

3. Restaurer les dependances :

```bash
dotnet restore
```

4. Appliquer les migrations sur la base SQLite :

```bash
dotnet ef database update
```

5. Compiler le projet :

```bash
dotnet build
```

6. Lancer l'application :

```bash
dotnet run
```

Adresse locale (selon votre environnement) :

```txt
http://localhost:5286
```

## 7. Lancer l'application avec Docker

```bash
docker build -t locatic:latest .
docker run -p 8080:8080 -v locatic-data:/data locatic:latest
```

L'application est alors accessible sur `http://localhost:8080`. Les donnees SQLite sont persistees dans le volume `locatic-data` (monte sur `/data` dans le conteneur).

## 8. Commandes utiles

Creer une migration :

```bash
dotnet ef migrations add NomMigration
```

Mettre a jour la base :

```bash
dotnet ef database update
```

Supprimer la derniere migration :

```bash
dotnet ef migrations remove
```

Compiler :

```bash
dotnet build
```

Lancer les tests :

```bash
dotnet test Locatic/Locatic.csproj
```

## 9. Configuration

La chaine de connexion SQLite est configuree dans :
- Locatic/appsettings.json
- Locatic/appsettings.Development.json

Valeur par defaut :

```txt
Data Source=agence.db
```

En conteneur, elle est surchargee par la variable d'environnement `ConnectionStrings__DefaultConnection` (voir `Dockerfile`), pointee vers le volume persistant `/data`.

## 10. Initialisation des donnees

Au demarrage, un seeding est execute pour inserer des donnees de base si elles n'existent pas encore (marques, modeles, voitures, clients).

## 11. Tests automatises

Un premier jeu de tests xUnit se trouve dans `Locatic/Tests/` (couche services avec EF Core InMemory). Il s'execute avec `dotnet test Locatic/Locatic.csproj` et fait partie du pipeline CI.

## 12. Integration continue (CI)

Le workflow `.github/workflows/ci.yml` s'execute sur chaque Pull Request (vers `develop`) et sur `main`/`develop` :
- `lint` : `dotnet format --verify-no-changes`
- `build` : `dotnet build` + `dotnet test` (xUnit)
- `security` : scan Trivy du code source (HIGH/CRITICAL)
- `docker-build` : build de l'image Docker + scan Trivy de l'image (workflow reutilisable)
- `publish` : **uniquement sur push vers `main`** publie `ghcr.io/pauldatcom/locatic:<sha>` et `:latest`

Le pipeline GitHub s'arrete volontairement apres la publication de l'image : le deploiement sur minikube est declenche localement (Terraform + Ansible), jamais depuis les runners GitHub. Detail dans `docs/ci-cd.md`.

## 13. Monitoring local

Une stack Prometheus/Grafana/Alertmanager est disponible via `docker-compose.yml` :

```bash
docker compose up -d
```

- Prometheus : http://localhost:9090
- Grafana : http://localhost:3001 (identifiants par defaut dans `docker-compose.yml`)
- Alertmanager : http://localhost:9093

Detail dans `docs/monitoring.md`.

## 14. Infrastructure et deploiement local (Terraform / Ansible / minikube)

Le dossier `infra/` contient la configuration Terraform (namespaces, PVC SQLite sur minikube) et le playbook Ansible (Gires) qui orchestrent le deploiement sur minikube. L'application est deployee derriere un reverse proxy Nginx et supervisee par Prometheus/Grafana. Etapes exactes dans `docs/terraform.md`, `docs/ansible.md`, `docs/kubernetes.md`, `docs/deploiement-local.md` et `docs/exploitation.md`.

Bonus : un chart Helm `chart/locatic/` est disponible (alternative a Kustomize avec rollback natif, voir `docs/helm.md`).

## 15. Bonnes pratiques Git

- Workflow a deux branches longues : `develop` (integration, cible des PRs) et `main` (stable, resultante des merges de `develop`).
- Les deux branches sont protegees : pas de push direct, Pull Request obligatoire avec checks CI au vert, pas de force-push.
- Toute PR cible `develop`. Les merges `develop` -> `main` se font via une PR de synchronisation.
- Verifier que le projet build et que les tests passent avant d'ouvrir une Pull Request.
- Ne pas versionner les fichiers de base locale et temporaires SQLite, ni les secrets, ni l'etat Terraform (voir `.gitignore`).

## 16. Documentation complementaire

- `docs/mini-project.md` : consigne complete du mini-projet
- `docs/consigne.md` : consigne du projet POO original
- `docs/architecture.md` : architecture globale, roles de chaque composant
- `docs/ci-cd.md` : regles de branche, Pull Requests, checks, jobs du pipeline, publication de l'image, limites du pipeline GitHub
- `docs/terraform.md` : ressources, variables, outputs, gestion de l'etat, procedure init/plan/apply sur minikube
- `docs/ansible.md` : playbook, etapes orchestrees, dependance aux outputs Terraform (Gires)
- `docs/kubernetes.md` : ressources K8s, services exposes, stockage SQLite, reverse proxy Nginx, configuration overlays
- `docs/helm.md` : chart Helm bonus, valeurs configurables, procedure de release, rollback
- `docs/monitoring.md` : Prometheus, Grafana, alertes (Gires)
- `docs/deploiement-local.md` : sequence complete image publiee -> application deployee sur minikube
- `docs/exploitation.md` : verification, logs, rollback, diagnostic
- `docs/preuves/` : captures et extraits de logs des etapes importantes

## 17. Auteurs

- Esso Mawaki ASSIAH
- Gires TIENTCHEU

Adaptation DevOps (CI/CD, Docker, Terraform, Ansible, Kubernetes, monitoring) : Paul (pauldatcom)
