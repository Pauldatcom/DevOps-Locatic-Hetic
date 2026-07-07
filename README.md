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
├── .github/workflows/   # Pipeline CI (build, tests, scan de securite)
├── Locatic/             # Code applicatif ASP.NET Core MVC
│   ├── Controllers/ Entities/ Data/ Services/ ViewModels/ Views/ wwwroot/
│   └── Tests/           # Tests automatises (xUnit)
├── Dockerfile           # Image de l'application
├── docker-compose.yml   # Stack de monitoring locale (Prometheus/Grafana/Alertmanager)
├── infra/
│   ├── terraform/       # Infrastructure locale (namespace, stockage, environnements)
│   ├── ansible/         # Orchestration du deploiement local
│   └── kubernetes/      # Manifests Kubernetes (app, Nginx, ConfigMap, PVC)
├── monitoring/          # Configuration Prometheus, Grafana, Alertmanager
└── docs/                # Documentation detaillee (architecture, ci-cd, terraform, ansible, kubernetes, monitoring, exploitation...)
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

Le workflow `.github/workflows/ci.yml` s'execute sur chaque Pull Request et sur `main` :
- build et tests .NET
- scan de securite du code (Trivy)

Le pipeline GitHub s'arrete volontairement apres ces controles : le build/scan/publication de l'image et le deploiement sur minikube sont geres localement (Terraform + Ansible), jamais depuis les runners GitHub. Detail dans `docs/ci-cd.md`.

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

Le dossier `infra/` contient la configuration Terraform (namespace, stockage) et le playbook Ansible qui orchestrent le deploiement sur minikube. Etapes exactes dans `docs/terraform.md`, `docs/ansible.md`, `docs/kubernetes.md` et `docs/deploiement-local.md`.

## 15. Bonnes pratiques Git

- La branche `main` est protegee : pas de push direct, Pull Request obligatoire avec au moins une approbation, pas de force-push ni de suppression de branche.
- Verifier que le projet build et que les tests passent avant d'ouvrir une Pull Request.
- Ne pas versionner les fichiers de base locale et temporaires SQLite, ni les secrets ou l'etat Terraform.

## 16. Documentation complementaire

- `docs/mini-project.md` : consigne complete du mini-projet
- `docs/architecture.md` : architecture globale
- `docs/ci-cd.md` : regles de branche, Pull Requests, pipeline CI
- `docs/terraform.md`, `docs/ansible.md`, `docs/kubernetes.md`, `docs/helm.md` : infrastructure et deploiement
- `docs/monitoring.md` : Prometheus, Grafana, alertes
- `docs/deploiement-local.md` : sequence complete de deploiement local
- `docs/exploitation.md` : verification, logs, rollback, diagnostic

## 17. Auteurs

- Esso Mawaki ASSIAH
- Gires TIENTCHEU

Adaptation DevOps (CI/CD, Docker, Terraform, Ansible, Kubernetes, monitoring) : Paul (pauldatcom)
