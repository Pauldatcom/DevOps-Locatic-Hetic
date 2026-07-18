# Deploiement local (image publiee -> application sur minikube)

Ce document decrit l'ordre **exact** des actions locales pour passer d'une
image Docker deja publiee par la CI (sur `ghcr.io`) a une application Locatic
deployee et fonctionnelle sur un cluster minikube. Le pipeline GitHub Actions
s'arrete apres la publication de l'image (voir `docs/ci-cd.md`) : tout ce qui
suit s'execute sur la machine de l'operateur.

## Vue d'ensemble du chemin

```
ghcr.io/pauldatcom/locatic:latest
        |
        | (1) docker pull
        v
docker daemon hote
        |
        | (2) minikube image load
        v
minikube (daemon Docker interne)
        |
        | (3) terraform apply    -> namespaces + PVC SQLite
        | (4) terraform output   -> infra/ansible/vars.json
        | (5) ansible-playbook    -> deploy/k8s/{app,nginx,monitoring}
        v
Pods Locatic + Nginx + Prometheus + Grafana sur minikube
        |
        | (6) kubectl port-forward ou NodePort Nginx
        v
http://<minikube-ip>:<node-port>  (utilisateur)
```

## Prerequis locaux

| Outil | Version mini | Rôle | Verification |
| --- | --- | --- | --- |
| Docker | 24+ | Pull de l'image depuis ghcr.io | `docker --version` |
| minikube | 1.30+ | Cluster Kubernetes local | `minikube version` |
| kubectl | 1.28+ | Pilotage du cluster | `kubectl version --client` |
| Terraform | 1.6+ | Infrastructure (namespaces, PVC) | `terraform version` |
| Ansible | 2.15+ | Orchestration du deploiement | `ansible --version` |
| Helm | 3.13+ | Bonus Helm / rollback | `helm version` (optionnel) |
| `jq` | 1.6+ | Lecture des outputs Terraform | `jq --version` (optionnel) |

Cluster minikube demarre :

```bash
minikube start --cpus=4 --memory=4g --disk=20g
# (les ressources sont des pistes ; adaptez a votre machine)

minikube status
kubectl config current-context   # doit renvoyer 'minikube'
```

## Etapes de deploiement

### Etape 0 (prealable une fois) : verifier qu'une image est publiee

L'image doit avoir ete publiee par le job `publish` du pipeline GitHub Actions
apres un merge sur `main` (voir `docs/ci-cd.md`).

```bash
# Verifier que le tag 'latest' existe sur ghcr.io
docker pull ghcr.io/pauldatcom/locatic:latest
docker images ghcr.io/pauldatcom/locatic
```

Si l'image n'est pas encore publiee (pipeline non passe), vous pouvez la
construire localement :

```bash
docker build -t ghcr.io/pauldatcom/locatic:latest .
```

### Etape 1 : charger l'image dans minikube

Sur minikube, le daemon Docker du cluster est isole de l'hote. Il faut donc
charger explicitement l'image dans minikube pour que les pods puissent
l'utiliser sans la tirer depuis ghcr.io (pas d'imagePullSecrets necessaire).

```bash
make load-image TAG=latest
# equivalent a :
./scripts/load-image.sh latest
```

Le script :

1. `docker pull ghcr.io/pauldatcom/locatic:latest` depuis ghcr.io ;
2. re-tag en `locatic:latest` (image locale) ;
3. `minikube image load locatic:latest` ;
4. verifie que l'image est visible dans minikube (`minikube image ls`).

> Pour epingler un SHA precis publie par la CI :
> `./scripts/load-image.sh a1b2c3d` puis adapter `app_tag` dans
> `infra/terraform/environments/<env>/terraform.tfvars`.

### Etape 2 : appliquer Terraform (namespaces + PVC SQLite)

Terraform prepare l'infrastructure : namespace applicatif, namespace
monitoring, PVC `locatic-sqlite`. Voir `docs/terraform.md` pour le detail.

```bash
# Soit via le Makefile :
make tf-apply ENV=dev

# Soit a la main :
cd infra/terraform/environments/dev
terraform init
terraform validate
terraform apply
```

Verification :

```bash
kubectl get namespaces
# doit afficher locatic-staging et monitoring

kubectl get pvc -n locatic-staging
# doit afficher locatic-sqlite en Bound
```

### Etape 3 : recuperer les outputs Terraform pour Ansible

Terraform produit une sortie `ansible_vars` aggregant les valeurs necessaires
a Ansible (namespace, PVC, image, tag, replicas...).

```bash
make tf-output ENV=dev
# equivalent a :
cd infra/terraform/environments/dev && terraform output -json ansible_vars > infra/ansible/vars.json
```

Le fichier `infra/ansible/vars.json` est genere (**gitignore**, jamais commite).
Exemple de contenu :

```json
{
  "app_namespace": "locatic-staging",
  "monitoring_namespace": "monitoring",
  "sqlite_pvc_name": "locatic-sqlite",
  "sqlite_mount_path": "/data",
  "app_image": "ghcr.io/pauldatcom/locatic:latest",
  "app_replicas": 2,
  "app_log_level": "debug",
  "kube_context": "minikube"
}
```

### Etape 4 : lancer Ansible (deploiement app + Nginx + monitoring)

Ansible lit `infra/ansible/vars.json` et applique les manifests Kubernetes
(app, Nginx, monitoring). Voir `docs/ansible.md` pour le playbook.

```bash
make ansible-deploy ENV=dev
# equivalent a :
ansible-playbook infra/ansible/playbook.yml \
  --extra-vars @infra/ansible/vars.json \
  --extra-vars environment=dev
```

Le playbook (fourni par Gires) doit :

- verifier les prequis (kubectl, helm, cluster actif) ;
- templatiser les manifests avec les valeurs de `vars.json` (namespace, image,
  tag, PVC, replicas) ;
- appliquer `deploy/k8s/app/overlays/dev` (Kustomize) ;
- appliquer `deploy/k8s/nginx/` (Nginx reverse proxy) ;
- appliquer `deploy/k8s/monitoring/` (Prometheus / Grafana / Alertmanager).

### Etape 5 : attendre que les pods soient prets

```bash
kubectl rollout status deploy/locatic -n locatic-staging --timeout=180s
kubectl get pods -n locatic-staging
kubectl get pods -n monitoring
```

### Etape 6 : tout en une commande (recommande)

Le script `scripts/deploy.sh` enchaine les etapes 2 a 5 automatiquement :

```bash
make deploy ENV=dev
# equivalent a :
./scripts/deploy.sh dev
```

Sortie attendue :

```
[deploy] minikube OK
[deploy] 1/4 Terraform init/apply dans .../environments/dev
[deploy] 2/4 Ecriture des outputs Terraform vers infra/ansible/vars.json
[deploy] 3/4 ansible-playbook (deploiement app + nginx + monitoring)
[deploy] 4/4 kubectl rollout status deploy/locatic dans locatic-staging
[deploy] Deploiement termine avec succes.
```

### Etape 7 : acceder a l'application via Nginx

L'application n'est pas exposee directement : Nginx est le point d'entree.

```bash
# Recuperer l'URL du Service Nginx (type NodePort sur minikube)
minikube service nginx -n locatic-staging --url
# ex. http://192.168.49.2:30080

# Ouvrir dans le navigateur, ou :
curl http://$(minikube ip):$(kubectl get svc nginx -n locatic-staging -o jsonpath='{.spec.ports[0].nodePort}')
```

L'application Locatic doit s'afficher. L'acces direct au Service `locatic`
(ClusterIP) doit etre bloque depuis l'exterieur du cluster : c'est la
contrainte forte du mini-projet (l'app n'est pas un point d'entree).

### Etape 8 : verifier le deploiement

```bash
make verify ENV=dev
# equivalent a :
./scripts/verify.sh dev
```

Le script verifie :

1. Pods Locatic running ;
2. PVC SQLite Bound ;
3. endpoint `/health` repond 200 ;
4. endpoint `/metrics` expose des metriques Prometheus ;
5. acces via Nginx (NodePort) ;
6. fichier `/data/agence.db` present dans le conteneur ;
7. targets Prometheus UP.

Pour tester la **persistance SQLite au redemarrage d'un pod** :

```bash
make verify-strict ENV=dev
# equivalent a :
./scripts/verify.sh dev --persistence-check
```

Le script redemarre le Deployment Locatic et verifie que le fichier
`/data/agence.db` conserve son `mtime` (preuve que le volume persistant
n'a pas ete efface).

### Acceder aux dashboards

Prometheus et Grafana sont dans le namespace `monitoring`.

```bash
# Prometheus
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# -> http://localhost:9090

# Grafana
kubectl port-forward -n monitoring svc/grafana 3000:3000
# -> http://localhost:3000 (admin / mot de passe defini par Gires)
```

Le dashboard `app-overview` doit afficher l'etat de chaque service (app,
Nginx, stockage, monitoring). Voir `docs/monitoring.md`.

## Bonus : deploiement via Helm

Si vous prefer utiliser le chart Helm (voir `docs/helm.md`) :

```bash
# Apres terraform apply (etape 2)
helm upgrade --install locatic chart/locatic \
  --namespace locatic-staging \
  -f chart/locatic/values.yaml \
  --set image.tag=latest \
  --set sqlite.pvc.name=locatic-sqlite
```

Rollback possible via :

```bash
make rollback REV=1 ENV=dev
# equivalent a :
helm rollback locatic 1 -n locatic-staging
```

## Deploiement en prod

Identique, avec `ENV=prod` :

```bash
make load-image TAG=<sha-epingle>
make deploy ENV=prod
make verify ENV=prod
```

En prod, on epingle `app_tag` a un SHA precis dans
`infra/terraform/environments/prod/terraform.tfvars` au lieu de `latest` pour
garantir la reproductibilite.

## Re-deploiement apres une nouvelle image publiee

Quand une nouvelle image est publiee sur ghcr.io (apres un merge sur `main`) :

```bash
# 1. Charger la nouvelle image
make load-image TAG=latest

# 2. Re-deployer (Terraform est idempotent, Ansible reapplique les manifests)
make deploy ENV=dev

# 3. Verifier
make verify ENV=dev
```

Si vous avez epingle un SHA en prod :

```bash
make load-image TAG=<nouveau-sha>
# Editez infra/terraform/environments/prod/terraform.tfvars -> app_tag = "<nouveau-sha>"
make deploy ENV=prod
```

## Probleme frequent : l'image n'est pas trouvee par le pod

Symptome : pod en `ErrImagePull` ou `ImagePullBackOff`.

Causes :

1. Oubli de `make load-image` -> l'image n'est pas dans minikube.
2. `imagePullPolicy: Always` dans le Deployment (par defaut c'est `IfNotPresent`
   dans nos manifests, mais si vous l'avez change, Kubernetes essaie de tirer
   depuis ghcr.io et echoue sans credentials).

Solutions :

```bash
minikube image ls | grep locatic
# Si l'image n'apparait pas :
make load-image TAG=latest

# Forcer le redemarrage du pod :
kubectl rollout restart deploy/locatic -n locatic-staging
```

## Recapitulatif des commandes

```bash
# Setup une fois
minikube start --cpus=4 --memory=4g

# Deploiement complet
make load-image TAG=latest
make deploy ENV=dev

# Verification
make verify ENV=dev

# Acces
minikube service nginx -n locatic-staging --url

# Re-deploiement apres nouvelle image
make load-image TAG=latest
make deploy ENV=dev
```