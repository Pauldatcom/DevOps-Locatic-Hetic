# Helm

## Statut

Le **bonus Helm n'est pas realise** dans cette version du projet.

Le deploiement Kubernetes est gere avec :

- manifests bruts + **Kustomize** (`deploy/k8s/{app,nginx,monitoring}`)
- orchestration **Ansible** (`infra/ansible/deploy-k8s.yml`)

Le provider Helm est declare dans Terraform (`hashicorp/helm`) comme reserve pour
une eventuelle evolution, mais **aucune release Helm n'est installee**.

## Si le bonus etait ajoute plus tard

Structure envisagee :

```txt
deploy/helm/locatic/
├── Chart.yaml
├── values.yaml
├── values-dev.yaml
├── values-prod.yaml
└── templates/
    ├── deployment.yaml
    ├── service.yaml
    ├── configmap.yaml
    └── ingress-or-nginx.yaml
```

Procedure type :

```bash
helm lint deploy/helm/locatic
helm upgrade --install locatic deploy/helm/locatic \
  -n locatic-staging -f deploy/helm/locatic/values-dev.yaml
helm rollback locatic 1 -n locatic-staging
```

Ansible pourrait alors appeler `helm upgrade --install` a la place de
`kubectl apply -k`.
