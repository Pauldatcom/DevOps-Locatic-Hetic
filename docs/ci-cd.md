# CI/CD

## Regles de branche

- Travail sur branches feature
- Integration via Pull Requests vers `develop` / `main`
- `main` doit etre protegee : pas de push direct, PR obligatoire, checks CI
  obligatoires avant merge

## Pipeline GitHub Actions

Fichier : `.github/workflows/ci.yml`

| Job | Declenchement | Role |
| --- | --- | --- |
| `lint` | PR + `main` | `dotnet format --verify-no-changes` |
| `build` | PR + `main` | restore, build, tests |
| `security` | apres build | Trivy filesystem sur `Locatic/` |
| `docker-build` | apres build | build image + scan Trivy image |
| `publish` | **push `main` uniquement** | push vers `ghcr.io/<owner>/locatic` |

Dependances explicites : `publish` attend `lint`, `security` et `docker-build`.
Le pipeline echoue si une etape critique echoue (`exit-code: 1` sur Trivy).

## Publication de l'image

- Registry : GitHub Container Registry (`ghcr.io`)
- Auth : `GITHUB_TOKEN` (secret GitHub, jamais en clair dans le repo)
- Tags : SHA du commit + `latest`
- Condition : `github.ref == 'refs/heads/main' && github.event_name == 'push'`

## Limites du pipeline GitHub

GitHub Actions **ne peut pas** deployer sur le minikube local de l'etudiant.

Apres la publication de l'image, le deploiement se fait **sur la machine locale** :

1. `terraform apply`
2. `ansible-playbook deploy-k8s.yml` (ou `scripts/deploy.sh`)

Aucune etape Terraform/Ansible/kubectl n'est executee dans la CI.

## Verification locale de la CI

Sur une PR, les checks doivent etre verts avant merge. Sur `main`, l'image doit
apparaitre dans les Packages GitHub du depot.
