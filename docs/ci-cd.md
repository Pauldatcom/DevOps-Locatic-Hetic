# CI / CD

Le pipeline d'integration et de deploiement continu repose sur **GitHub
Actions**. Ce document decrit les regles de branche, le fonctionnement des
Pull Requests, les checks obligatoires, les jobs du pipeline, la publication
de l'image Docker et les limites volontaires du pipeline GitHub.

## Workflow de branche

Le depot suit un modele a deux branches longues :

| Branche | Rôle | Protection |
| --- | --- | --- |
| `main`   | Branche stable, resultante des merges de `develop`. | Protegee : pas de push direct, PR obligatoire, checks CI obligatoires, pas de force-push. |
| `develop`| Branche d'integration, cible de toutes les PRs dev. | Protegee : pas de push direct, PR obligatoire, checks CI obligatoires. |

Toute modification passe par une **Pull Request** vers `develop`. Quand
`develop` est stable, elle est mergee vers `main` via une PR de synchronisation.

Les branches de feature suivent la convention `feat/*`, `fix/*`, `docs/*`,
`chore/*`.

## Pull Requests

- Une PR doit obligatoirement etre basee sur `develop` (ou une branche de
  release dediee).
- Les checks CI doivent passer au vert avant merge.
- Au moins une revue est requise (configurable cote GitHub).
- L'historique reste lisible : preferer des commits atomiques avec messages
  explicites (`type(scope): message`).
- Pas de force-push sur les branches protegees.

## Checks obligatoires

Les jobs suivants doivent passer avant tout merge (configure dans les regles
de protection de branche cote GitHub) :

| Job | Rôle |
| --- | --- |
| `lint`         | `dotnet format --verify-no-changes --severity error` sur `Locatic/Locatic.csproj` |
| `build`       | `dotnet restore` + `dotnet build -c Release` + `dotnet test` (xUnit) |
| `security`     | Scan Trivy du filesystem `Locatic/` (severite HIGH, CRITICAL) |
| `docker-build` | Build de l'image Docker + scan Trivy de l'image (workflow reutilisable) |

Le job `publish` n'est pas obligatoire pour valider une PR : il ne s'execute
que sur `main` (voir ci-dessous).

## Fichier de pipeline

Le pipeline est defini dans [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)
avec un workflow reutilisable pour le build Docker dans
[`.github/workflows/reusable-docker.yml`](../.github/workflows/reusable-docker.yml).

### Declencheurs

```yaml
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]
```

### Organisation des jobs

```
lint ───────────────────────────┐
                                 ├──> publish (only main push)
build ──> security ──> docker-build
```

- `lint` et `build` tournent en parallele (pas de dependance entre eux).
- `security` depend de `build` (on scanne le code compile).
- `docker-build` depend de `build` (l'image ne se construit que si les tests
  passent).
- `publish` depend de `lint`, `security` et `docker-build` et ne s'execute
  **que sur un push vers `main`**.

### Job `lint`

```yaml
- run: dotnet restore Locatic/Locatic.csproj
- run: dotnet format Locatic/Locatic.csproj --verify-no-changes --severity error
```

Echoue si le code n'est pas formate selon les regles .NET. Permet de garder
un style coherent entre contributeurs sans辩论 de formatage.

### Job `build`

```yaml
- run: dotnet restore Locatic/Locatic.csproj
- run: dotnet build Locatic/Locatic.csproj --no-restore --configuration Release
- run: dotnet test Locatic/Locatic.csproj --no-build --configuration Release --verbosity normal
```

Compile en Release et execute les tests xUnit (2 tests initiaux dans
`Locatic/Tests/`). Le pipeline echoue si un test echoue.

### Job `security`

```yaml
- uses: aquasecurity/trivy-action@v0.36.0
  with:
    scan-type: fs
    scan-ref: Locatic/
    severity: HIGH,CRITICAL
    ignore-unfixed: true
    exit-code: 1
    format: table
```

Scan statique du code source `Locatic/` avec Trivy. Detecte les vulnérabilites
connues dans les dependances et le code. Echec du job si une vulnerabilite
HIGH ou CRITICAL non corrigee est detectee.

### Job `docker-build` (reutilisable)

Workflow [`.github/workflows/reusable-docker.yml`](../.github/workflows/reusable-docker.yml)
appele via `uses: ./.github/workflows/reusable-docker.yml`. Etapes :

1. `docker/metadata-action` : genere les tags (SHA, branche, semver).
2. `docker/setup-buildx-action` : configure Buildx pour le cache.
3. `docker/build-push-action` : build l'image **sans la pousser** (`push: false`,
   `load: true`) — l'image est chargee dans le runner pour etre scannee.
4. `aquasecurity/trivy-action` : scan de l'image construite (severite
   HIGH/CRITICAL).

Le build utilise le cache GitHub Actions (`cache-from: type=gha`,
`cache-to: type=gha,mode=max`) pour accelerer les builds successifs.

### Job `publish`

```yaml
if: github.ref == 'refs/heads/main' && github.event_name == 'push'
```

S'execute **uniquement sur un push vers `main`** (pas sur les PRs, pas sur
`develop`). Etapes :

1. Login a `ghcr.io` avec `${{ secrets.GITHUB_TOKEN }}` (token automatique,
   aucune secret a gerer).
2. `docker/metadata-action` : tags generes = SHA court (`type=sha,prefix=`)
   + `latest` (`type=raw,value=latest`).
3. `docker/build-push-action` : build + **push** vers
   `ghcr.io/pauldatcom/locatic:<sha>` et `ghcr.io/pauldatcom/locatic:latest`.

A la fin de ce job, l'image est disponible publiquement (ou privement selon
les settings du depot) sur ghcr.io.

## Image Docker publiee

- **Registry** : GitHub Container Registry (ghcr.io).
- **Nom** : `ghcr.io/pauldatcom/locatic`.
- **Tags** :
  - `:<sha-court>` : tag immutable correspondant au commit merge sur `main`.
  - `:latest` : pointe vers le dernier build de `main`.
- **Dockerfile** : [`Dockerfile`](../Dockerfile) a la racine du depot.
  Multi-stage (build SDK 8.0 -> runtime aspnet:8.0), utilisateur non-root,
  volume `/data`, HEALTHCHECK sur `/health`.

## Limites volontaires du pipeline GitHub

La consigne du mini-projet (voir [`docs/mini-project.md`](mini-project.md) §18
et §132) impose une limite forte : **le pipeline GitHub ne deploie pas sur
minikube**. Raison technique : minikube tourne sur la machine de l'operateur,
les runners GitHub n'ont pas acces a cette machine.

Le pipeline GitHub s'arrete donc **apres la publication de l'image**. Il ne
realise **pas** :

- `terraform apply` ;
- `ansible-playbook` ;
- `kubectl apply` ;
- `minikube image load` ;
- `helm upgrade --install`.

Toutes ces operations sont executees **localement** par l'operateur, apres
avoir recupere l'image publiee sur ghcr.io. La procedure complete est dans
[`docs/deploiement-local.md`](deploiement-local.md).

Cette separation Garantit que :

- le pipeline ne depend pas d'un cluster local qui n'existerait pas sur le
  runner ;
- l'operateur garde le controle du moment ou il deploie ;
- aucun secret d'acces au cluster n'est stocke dans GitHub.

## Gestion des secrets

Aucun secret n'est commite dans le depot. Les secrets utilises par la CI sont
exclusivement fournis par GitHub :

| Secret | Source | Usage |
| --- | --- | --- |
| `GITHUB_TOKEN` | Automatique (GitHub) | Login a ghcr.io pour `publish`. |

Aucun token personnel, mot de passe ou cle privee n'est necessaire. Si un jour
un secret devait etre ajoute (registry privee, webhook d'alerte...), il le
serait via `Settings -> Secrets -> Actions` et jamais dans le code.

Cote local, le kubeconfig de minikube reste sur la machine operateur
(`~/.kube/config`) et n'est jamais commit. Voir aussi `.gitignore` qui exclut
`.env`, `*.tfstate`, `*.tfvars.local`, etc.

## Verifier le pipeline localement

Avant d'ouvrir une PR, on peut reproduire les checks CI localement :

```bash
# lint
dotnet format Locatic/Locatic.csproj --verify-no-changes --severity error

# build + tests
dotnet restore Locatic/Locatic.csproj
dotnet build  Locatic/Locatic.csproj -c Release
dotnet test   Locatic/Locatic.csproj --no-build -c Release

# build image (equivalent docker-build)
docker build -t locatic:ci .

# scan image (equivalent security)
trivy image --severity HIGH,CRITICAL --ignore-unfixed locatic:ci
```

Si tout passe localement, la PR passera au vert sur GitHub.