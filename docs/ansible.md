# Ansible — Locatic

Ce dossier orchestre le déploiement de l'application **Locatic** sur un cluster Kubernetes local (**kind**), à partir de l'infrastructure de base provisionnée par Terraform (namespace + PersistentVolumeClaim).

## Structure

```
infra/ansible/
├── inventory.yml              # Inventaire (localhost, exécution en local)
├── requirements.yml           # Collections Ansible requises
├── deploy-k8s.yml             # Playbook principal : déploiement K8s
├── site.yml                   # Playbook legacy (config VM, non utilisé pour K8s)
├── bootstrap-python.yml       # Playbook legacy (bootstrap Python sur VM)
├── roles/
│   ├── k8s_deploy/            # Rôle principal : build + déploiement K8s
│   │   ├── defaults/main.yml  # Variables (image, namespace, chemins...)
│   │   └── tasks/main.yml     # Tâches : build, load, apply
│   ├── base/                  # Rôle legacy (provisioning VM)
│   └── nginx/                 # Rôle legacy (nginx natif sur VM)
```

## Prérequis

- Docker Desktop installé, avec l'**intégration WSL activée** pour ta distro (Settings → Resources → WSL Integration)
- Un cluster **kind** existant et démarré (`kind get clusters` doit lister `kind`)
- Python 3 avec le module `kubernetes` installé (`pip install kubernetes`)
- Ansible installé (`ansible --version`)

## Installation des dépendances (une seule fois)

```bash
cd infra/ansible

# Collection Ansible pour piloter Kubernetes
ansible-galaxy collection install -r requirements.yml

# Module Python requis par la collection kubernetes.core
pip install kubernetes
```

## Variables (`roles/k8s_deploy/defaults/main.yml`)

| Variable                  | Description                                              | Valeur par défaut |
|---------------------------|------------------------------------------------------------|--------------------|
| `locatic_image_name`      | Nom:tag de l'image Docker construite                       | `locatic:latest` |
| `locatic_docker_context`  | Dossier contenant le `Dockerfile` (contexte de build)      | racine du repo |
| `kind_cluster_name`       | Nom du cluster kind cible                                  | `kind` |
| `k8s_namespace`           | Namespace Kubernetes où déployer l'app                     | `locatic-staging` |
| `k8s_manifests_dir`       | Dossier contenant les manifests K8s bruts                  | `infra/kubernetes/base` |

Ajuste ces valeurs si ton environnement diffère (nom de cluster, chemin du Dockerfile...).

## Ce que fait le rôle `k8s_deploy`

1. **Build** de l'image Docker de l'application (`docker build`)
2. **Chargement** de l'image dans le cluster kind (`kind load docker-image`) — nécessaire car kind ne peut pas puller une image locale directement
3. **Application** des `ConfigMap` (config app + config nginx)
4. **Déploiement** de l'application Locatic (Deployment + Service)
5. **Déploiement** de nginx en reverse-proxy (Deployment + Service NodePort)
6. **Attente** que les deux Deployments soient `Available` avant de terminer

## Lancer le déploiement

```bash
cd infra/ansible
ansible-playbook -i inventory.yml deploy-k8s.yml
```

Sortie attendue : `ok=7 changed=5 unreachable=0 failed=0`.

## Vérifier le déploiement

```bash
kubectl get all -n locatic-staging
kubectl get pvc -n locatic-staging
```

Le PVC (`locatic-sqlite`, créé par Terraform) doit passer de `Pending` à `Bound` une fois que le pod de l'app le monte.

## Accéder à l'application

Le cluster kind utilisé ici n'expose pas nativement les NodePorts sur `localhost`. Utilise un port-forward :

```bash
kubectl port-forward -n locatic-staging svc/locatic-nginx 8888:80
```

Puis dans un navigateur ou un autre terminal :
```bash
curl http://localhost:8888/health
```

## Problèmes fréquents

| Symptôme | Cause probable | Solution |
|---|---|---|
| `The command 'docker' could not be found` | Intégration WSL de Docker Desktop désactivée/perdue | Docker Desktop → Settings → Resources → WSL Integration → activer la distro, Apply & Restart |
| `context deadline exceeded` sur un PVC | `wait_until_bound` bloque avec une StorageClass en `WaitForFirstConsumer` | Mettre `wait_until_bound = false` (déjà fait côté Terraform) |
| Build Docker tué (`exit code 147`) | Manque de RAM allouée à WSL | Configurer `C:\Users\<user>\.wslconfig` avec une limite mémoire adaptée, puis `wsl --shutdown` |
| `context "X" does not exist` | `kube_context` ne correspond pas au contexte kubectl réel | Vérifier avec `kubectl config get-contexts` et aligner la variable |

## Rôles legacy (`base`, `nginx`)

Ces rôles proviennent d'un projet d'école basé sur des VMs provisionnées par Terraform et configurées en SSH (`site.yml`, `bootstrap-python.yml`). Ils ne sont **pas utilisés** dans le déploiement Kubernetes de Locatic et sont conservés à titre de référence.
