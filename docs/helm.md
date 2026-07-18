# Helm (bonus)

Le chart Helm `chart/locatic/` est un **bonus** (voir `docs/mini-project.md`
§Bonus). Il permet de deployer l'application Locatic avec une seule commande
`helm upgrade --install` au lieu d'appliquer les manifests Kustomize un par
un. Ansible peut appeler Helm pour orchestrer le deploiement.

Le chart est **optionnel** : le chemin nominal reste Terraform + Ansible +
Kustomize (`deploy/k8s/app/`). Le chart Helm est une alternative
parametree plus dense, surtout utile pour le rollback (bonus +1 pt).

## Structure du chart

```
chart/locatic/
├── Chart.yaml          # Metadonnees du chart (name, version, appVersion)
├── values.yaml         # Valeurs par defaut configurables
└── templates/
    ├── NOTES.txt       # Notes affichees apres install
    ├── configmap.yaml  # ConfigMap des variables d'env non sensibles
    ├── secret.yaml     # Secret (vide par defaut - valorise via --set)
    ├── pvc.yaml        # PVC SQLite (creee si sqlite.pvc.create=true)
    ├── deployment.yaml # Deployment Locatic (probes, resources, volume)
    └── service.yaml    # Service ClusterIP
```

## Valeurs configurables (`values.yaml`)

| Cle | Default | Rôle |
| --- | --- | --- |
| `replicaCount`                 | `2`                 | Nombre de pods |
| `image.repository`             | `ghcr.io/pauldatcom/locatic` | Image Docker |
| `image.tag`                    | `latest`            | Tag (epingle SHA en prod) |
| `image.pullPolicy`             | `IfNotPresent`      | Strategie de pull |
| `securityContext`              | uid 10001 non-root  | Doit matcher le Dockerfile |
| `resources`                    | 100m/128Mi -> 500m/512Mi | Requests/limits CPU/mem |
| `probes.liveness/readiness/startup` | `/health` | Probes de sante |
| `sqlite.mountPath`             | `/data`             | Chemin de montage du volume |
| `sqlite.pvc.create`            | `false`             | Si true, le chart cree la PVC ; sinon reference celle creee par Terraform |
| `sqlite.pvc.name`              | `locatic-sqlite`    | Nom de la PVC a monter |
| `sqlite.pvc.size`              | `1Gi`               | Taille si creee par le chart |
| `config`                       | map d'env           | Variables d'env injectees via ConfigMap |
| `secrets`                      | `{}`                | Secrets injectes via Secret (toujours via `--set`, jamais commits) |
| `service.type`                 | `ClusterIP`         | Type de Service (Nginx reste l'entree) |
| `service.port`                  | `8080`              | Port du Service |
| `nginx.enabled`                | `false`             | Nginx gere par un module separe de Gires |
| `monitoring.prometheusScrape`  | `true`              | Annotations Prometheus sur le pod |

## Procedure de release

Prerequis : `terraform apply` a deja cree le namespace `locatic-staging` et la
PVC `locatic-sqlite` (voir `docs/terraform.md`).

```bash
# 1. Linter le chart
helm lint chart/locatic

# 2. Previsualiser les manifests generes (sans appliquer)
helm template locatic chart/locatic \
  --namespace locatic-staging \
  -f chart/locatic/values.yaml

# 3. Installer ou mettre a jour la release
helm upgrade --install locatic chart/locatic \
  --namespace locatic-staging \
  --create-namespace \
  -f chart/locatic/values.yaml \
  --set image.tag=latest

# 4. Verifier l'etat de la release
helm status locatic -n locatic-staging

# 5. Consulter l'historique des revisions
helm history locatic -n locatic-staging
```

## Override par environnement

On peut fournir un fichier de valeurs par environnement :

`chart/locatic/values-prod.yaml` (exemple, non fourni par defaut) :

```yaml
replicaCount: 3
image:
  tag: "a1b2c3d"   # SHA epingle publie par la CI
resources:
  requests:
    cpu: 200m
    memory: 256Mi
sqlite:
  pvc:
    size: 2Gi
```

```bash
helm upgrade --install locatic chart/locatic \
  --namespace locatic-prod \
  -f chart/locatic/values.yaml \
  -f chart/locatic/values-prod.yaml
```

## Integration avec Ansible

Ansible peut appeler Helm pour deployer le chart au lieu d'appliquer les
manifests Kustomize. Exemple de tache Ansible :

```yaml
- name: Deploy locatic via Helm
  kubernetes.core.helm:
    name: locatic
    chart_ref: "{{ playbook_dir }}/../chart/locatic"
    release_namespace: "{{ app_namespace }}"
    create_namespace: false  # le namespace est cree par Terraform
    values:
      replicaCount: "{{ app_replicas }}"
      image:
        tag: "{{ app_image_tag }}"
      sqlite:
        pvc:
          name: "{{ sqlite_pvc_name }}"  # PVC creee par Terraform
```

Voir `docs/ansible.md` pour l'integration complete.

## Rollback

Helm garde l'historique des revisions par release. Pour revenir a une version
anterieure :

```bash
# Lister les revisions
helm history locatic -n locatic-staging
# REVISION        STATUS          CHART           APP VERSION     DESCRIPTION
# 1               superseded      locatic-0.1.0   8.0             Install complete
# 2               deployed        locatic-0.1.0   8.0             Upgrade complete

# Revenir a la revision 1
helm rollback locatic 1 -n locatic-staging

# Verifier
helm status locatic -n locatic-staging
kubectl get pods -n locatic-staging -l app.kubernetes.io/instance=locatic
```

> **Important** : le rollback remet les manifests Kubernetes dans l'etat de la
> revision cible, mais ne restaure **pas** les donnees SQLite (le volume
> persistant conserve l'etat courant). Le rollback est donc safe pour
> l'application, mais n'annule pas les ecritures en base.

## Relation avec Kustomize (`deploy/k8s/app/`)

Le depot fournit **deux chemins** pour deployer l'application :

| Chemin | Avantages | Quand l'utiliser |
| --- | --- | --- |
| Kustomize (`deploy/k8s/app/`) | Simple, lisible, pas d'outil supplementaire | Chemin nominal, integre par Ansible |
| Helm (`chart/locatic/`) | Rollback natif, templating riche, historique | Bonus, ou si on veut le rollback documente |

Les deux ciblent le meme namespace et la meme PVC (creee par Terraform). En
production on choisira l'un **ou** l'autre, pas les deux simultanement
(conflit sur les ressources).

## Limites

- Le chart ne deploie **pas** Nginx ni le monitoring : `nginx.enabled=false`
  et `monitoring.enabled=false` par defaut. Ces composants restent geres par
  les manifests dedies de Gires (`deploy/k8s/nginx/` et
  `deploy/k8s/monitoring/`).
- Le chart ne cree pas le namespace (Terraform s'en charge) ; utiliser
  `--create-namespace=false` pour eviter qu'Helm ne le recree avec une config
  differente.
- Les secrets doivent toujours etre passes via `--set` ou `--set-file`, jamais
  ecrits en clair dans `values.yaml` (qui est commite).