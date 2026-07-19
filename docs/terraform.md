# Terraform

Terraform prepare l'infrastructure **locale** necessaire au deploiement de
Locatic sur un cluster minikube. Il ne deploie pas l'application ni Nginx ni le
monitoring : c'est le role d'Ansible (voir `docs/ansible.md`). Terraform se
concentre sur les ressources d'infrastructure : namespaces Kubernetes et volume
persistant pour SQLite.

## Pourquoi Terraform cible minikube

Le pipeline GitHub Actions s'arrete apres la publication de l'image Docker
(voir `docs/ci-cd.md`). Le deploiement final se fait sur le cluster minikube
qui tourne sur la machine de l'operateur. Terraform prepare donc ce cluster
local :

- cree les namespaces Kubernetes dedies a l'application et au monitoring ;
- provisionne le volume persistant utilise par SQLite ;
- produit les outputs consommes par Ansible pour la suite du deploiement.

Aucun cloud public n'est utilise. Le provider Kubernetes lit le kubeconfig
local (`~/.kube/config`) et cible le contexte `minikube`.

## Structure

```
infra/terraform/
├── modules/
│   ├── namespace/        # Namespaces Kubernetes (app + monitoring)
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   └── storage/          # PV (optionnelle) + PVC pour SQLite
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
└── environments/
    ├── dev/              # Environnement de developpement (staging)
    │   ├── main.tf       # Providers, variables, modules, outputs
    │   └── terraform.tfvars
    └── prod/             # Environnement de production local
        ├── main.tf
        └── terraform.tfvars
```

La separation `modules/` (ressources reutilisable) et `environments/` (racines
d'execution) permet d'isoler l'etat Terraform par environnement. Chaque
dossier `environments/*` possede son propre `terraform.tfstate` (local, jamais
commite).

## Providers

| Provider | Version | Rôle |
| --- | --- | --- |
| `hashicorp/kubernetes` | `~> 2.35` | Namespaces, PV, PVC |
| `hashicorp/helm`       | `~> 3.0`  | Reserve au bonus Helm (release pilotee par Ansible, voir `docs/helm.md`) |

Les providers sont configures via deux variables :

| Variable | Default | Rôle |
| --- | --- | --- |
| `kubeconfig_path` | `~/.kube/config` | Chemin du kubeconfig genere par `minikube start` |
| `kube_context`    | `minikube`        | Contexte Kubernetes a utiliser |

Aucun token ni secret n'est ecrit dans Terraform : tout passe par le kubeconfig
local de l'operateur.

## Ressources gerees

### Module `namespace`

- `kubernetes_namespace.app` : namespace applicatif `${app_name}-${environment}`
  (ex. `locatic-staging`, `locatic-prod`).
- `kubernetes_namespace.monitoring` : namespace `monitoring` dedie a la stack
  Prometheus / Grafana / Alertmanager.

Tous les namespaces portent les labels standards
`app.kubernetes.io/name`, `app.kubernetes.io/environment`,
`app.kubernetes.io/component` et `managed-by=terraform`.

### Module `storage`

- `kubernetes_persistent_volume_claim.sqlite` : PVC `${app_name}-sqlite` dans le
  namespace applicatif, montee sur `/data` par le pod Locatic. Mode d'acces
  `ReadWriteOnce` (SQLite = un seul writer).
- `kubernetes_persistent_volume.sqlite` : PV optionnelle, creee uniquement si
  `sqlite_host_path` est renseigne. Par defaut, on s'appuie sur la StorageClass
  `standard` fournie par minikube (provisioning dynamique). Le `reclaim_policy`
  est `Retain` pour ne pas perdre la base SQLite entre les deploiements.

## Variables principales

| Variable | Description | Default |
| --- | --- | --- |
| `app_name`           | Nom de l'application (Locatic) | — |
| `environment`        | Environnement (`staging` en dev, `prod` en prod) | — |
| `app_image`          | Image Docker publiee par la CI (`ghcr.io/pauldatcom/locatic`) | — |
| `app_tag`            | Tag de l'image (`latest` ou SHA epingle) | `latest` |
| `app_replicas`       | Nombre de replicas du Deployment | `1` (SQLite ReadWriteOnce) |
| `sqlite_size`        | Taille de la PVC SQLite | `1Gi` (dev) / `2Gi` (prod) |
| `sqlite_host_path`   | hostPath pour PV explicite (vide = StorageClass standard) | `""` |
| `monitoring_namespace` | Nom du namespace de monitoring | `monitoring` |
| `kube_context`       | Contexte Kubernetes minikube | `minikube` |
| `kubeconfig_path`    | Chemin du kubeconfig | `~/.kube/config` |
| `app_log_level`      | Niveau de log transmis a l'app | `debug` / `info` |

Toutes les variables sont documentees en commentaire dans chaque
`environments/*/main.tf`. Aucune variable ne contient de secret : il n'y a pas
de mot de passe dans Terraform (pas de base de donnees externe, SQLite est un
fichier dans le volume).

## Outputs

Terraform produit les outputs suivants, consommes par Ansible via
`terraform output -json` :

| Output | Exemple | Usage Ansible |
| --- | --- | --- |
| `app_namespace`        | `locatic-staging`   | Namespace des manifests applicatifs |
| `monitoring_namespace` | `monitoring`        | Namespace des manifests de monitoring |
| `sqlite_pvc_name`      | `locatic-sqlite`    | Reference `persistentVolumeClaim` dans le Deployment |
| `sqlite_mount_path`    | `/data`             | `volumeMounts.mountPath` du conteneur Locatic |
| `app_image`            | `ghcr.io/pauldatcom/locatic:latest` | `spec.template.spec.containers[].image` |
| `app_replicas`         | `1`                 | `spec.replicas` (force a 1 pour SQLite) |
| `app_log_level`        | `debug`             | ConfigMap / env de l'application |
| `kube_context`         | `minikube`          | Contexte `kubectl` a cibler |
| `ansible_vars`         | objet JSON          | Sortie agregee destinee a `ansible/vars.json` |

Le script `scripts/deploy.sh` (voir `docs/deploiement-local.md`) ecrit
`ansible/vars.json` a partir de `terraform output -json ansible_vars`. Ansible
lit ensuite ce fichier pour templatiser les manifests Kubernetes.

## Gestion de l'etat

- **Backend local** : l'etat `terraform.tfstate` reste dans
  `environments/<env>/terraform.tfstate` sur la machine de l'operateur.
- **Jamais commite** : `.gitignore` exclut `*.tfstate`, `*.tfstate.*`,
  `.terraform.lock.hcl` et les repertoires `.terraform/`.
- **Pas de backend distant** : le projet cible un deploiement 100 % local sur
  minikube ; un backend S3/GCS n'apporterait rien et ajouterait des secrets a
  gerer.
- **Pas de secrets dans l'etat** : aucune variable sensible n'est utilisee
  (pas de mot de passe de base de donnees, SQLite est un fichier). L'etat ne
  contient donc aucune donnee critique.

> Si un jour une variable sensible doit etre ajoutee, utiliser `sensitive = true`
> sur la variable et la passer via `TF_VAR_xxx` ou un fichier `*.auto.tfvars`
> ignore par Git, jamais directement dans `terraform.tfvars`.

## Procedure d'execution

Prerequis : `minikube start` deja execute, contexte `minikube` actif
(`kubectl config current-context` doit renvoyer `minikube`).

### Environnement dev

```bash
cd infra/terraform/environments/dev

# 1. Initialiser les providers (cree .terraform/ — ignore par Git)
terraform init

# 2. Reformater et valider
terraform fmt -recursive
terraform validate

# 3. Previsualiser les changements
terraform plan

# 4. Appliquer (cree les namespaces et la PVC SQLite sur minikube)
terraform apply

# 5. Recuperer les outputs pour Ansible
terraform output -json ansible_vars > ../../ansible/vars.json
# ou via le script de deploiement :
#   ../../../../scripts/deploy.sh dev
```

### Environnement prod

```bash
cd infra/terraform/environments/prod
terraform init
terraform plan
terraform apply
terraform output -json ansible_vars > ../../ansible/vars.json
```

En prod, on epingle `app_tag` a un SHA precis publie par la CI au lieu de
`latest` pour garantir la reproductibilite.

## Verification post-apply

```bash
kubectl get namespaces
# doit afficher locatic-staging (ou locatic-prod) et monitoring

kubectl get pvc -n locatic-staging
# doit afficher locatic-sqlite en Bound
```

Si la PVC reste `Pending`, verifier que la StorageClass `standard` existe :
`kubectl get storageclass` (minikube la fournit par defaut). Sinon, renseigner
`sqlite_host_path` dans `terraform.tfvars` pour forcer une PV hostPath.

## Limites connues

- Terraform ne deploie pas l'application, Nginx ni Grafana : c'est le role
  d'Ansible (voir `docs/ansible.md`).
- Le backend est local : si deux operateurs travaillent sur le meme
  environnement, ils doivent se coordonner pour ne pas ecraser l'etat de
  l'autre. Acceptable pour ce projet (un seul operateur, machine locale).
- La PV hostPath n'est utile que sur un cluster single-node comme minikube ; en
  multi-node il faudrait une StorageClass reseau.