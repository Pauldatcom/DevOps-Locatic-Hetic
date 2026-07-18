# Kubernetes

Ce dossier documente la partie Kubernetes du mini-projet Locatic. Les manifests
versionnes se trouvent dans `infra/kubernetes/base` et sont prevus pour etre
appliques sur le cluster minikube local apres la preparation du namespace et du
stockage par Terraform.

## Ressources fournies

- `configmap.yaml` contient la configuration de l'application ASP.NET Core et
  la configuration Nginx utilisee comme reverse proxy.
- `pvc.yaml` declare le volume persistant `locatic-sqlite-data` utilise par
  SQLite dans `/data/agence.db`.
- `app.yaml` declare le `Deployment` de l'application Locatic et son `Service`
  interne `ClusterIP`.
- `nginx.yaml` declare le `Deployment` Nginx et son `Service` `NodePort`, qui
  sert de point d'entree utilisateur.
- `kustomization.yaml` regroupe les manifests pour faciliter l'application par
  `kubectl` ou par Ansible.

## Architecture Kubernetes

L'application n'est pas exposee directement a l'utilisateur. Le flux attendu est
le suivant :

```txt
Utilisateur -> Service locatic-nginx -> Pod Nginx -> Service locatic-app -> Pod Locatic -> PVC SQLite
```

Le service `locatic-app` reste en `ClusterIP`. Le service `locatic-nginx` est le
point d'entree principal et expose le reverse proxy via `NodePort` pour minikube.

## Configuration applicative

Les variables principales sont definies dans la ConfigMap
`locatic-app-config` :

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection=Data Source=/data/agence.db`

Le chemin `/data` est monte depuis le PVC `locatic-sqlite-data`, ce qui permet
de conserver la base SQLite apres un redemarrage du pod applicatif.

## Image et tag

L'image par defaut est `locatic:latest`, pratique pour les tests avec l'image
chargee dans minikube. Apres publication dans la registry, Ansible ou une
commande Kustomize peut remplacer l'image :

```bash
kubectl set image deployment/locatic-app locatic=<registry>/<image>:<tag> -n locatic
```

Avec Kustomize :

```bash
kubectl kustomize infra/kubernetes/base
kubectl apply -k infra/kubernetes/base
```

## Probes et ressources

Le pod applicatif expose `/health`, deja configure dans ASP.NET Core. Les probes
Kubernetes utilisent cet endpoint pour verifier la disponibilite et la vie du
conteneur. Nginx utilise aussi `/health`, mais en passant par le reverse proxy :
si l'application ne repond plus, l'entree Nginx devient non prete.

Des requests et limits CPU/memoire sont definies pour l'application et Nginx afin
d'eviter un deploiement sans bornes de ressources.

## Verification locale

Apres execution de Terraform puis Ansible, les commandes utiles sont :

```bash
kubectl get all -n locatic
kubectl get pvc -n locatic
kubectl describe deployment locatic-app -n locatic
kubectl describe deployment locatic-nginx -n locatic
kubectl logs deploy/locatic-app -n locatic
kubectl logs deploy/locatic-nginx -n locatic
minikube service locatic-nginx -n locatic
```

Pour verifier que l'application passe bien par Nginx :

```bash
kubectl port-forward svc/locatic-nginx 8080:80 -n locatic
curl http://localhost:8080/health
```

Le service applicatif peut etre teste ponctuellement depuis le cluster, mais il
ne doit pas etre utilise comme point d'entree principal.

## Monitoring

Les manifests ajoutent des annotations Prometheus sur le service et le pod
applicatif pour preparer la collecte de `/metrics`. La partie monitoring
Prometheus/Grafana reste separee afin que le playbook Ansible puisse orchestrer
l'ensemble du deploiement local.
