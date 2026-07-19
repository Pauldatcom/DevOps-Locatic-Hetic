app_name    = "locatic"
environment = "staging"

# Image publiee par la CI sur ghcr.io (voir .github/workflows/ci.yml).
app_image    = "ghcr.io/pauldatcom/locatic"
app_tag      = "latest"
# SQLite ReadWriteOnce : 1 replica (aligne avec deploy/k8s/app)
app_replicas = 1

# Stockage SQLite
sqlite_size      = "1Gi"
sqlite_host_path = "" # StorageClass 'standard' de minikube

# Monitoring
monitoring_namespace = "monitoring"

# Contexte Kubernetes local (consigne = minikube).
# Si vous utilisez kind : kube_context = "kind-kind"
kube_context    = "minikube"
kubeconfig_path = "~/.kube/config"

app_log_level = "debug"