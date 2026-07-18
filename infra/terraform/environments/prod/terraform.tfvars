app_name    = "locatic"
environment = "prod"

# Image publiee par la CI sur ghcr.io. En prod on epingle un SHA precis.
app_image    = "ghcr.io/pauldatcom/locatic"
app_tag      = "latest"
app_replicas = 3

# Stockage SQLite
sqlite_size      = "2Gi"
sqlite_host_path = "" # StorageClass 'standard' de minikube

# Monitoring
monitoring_namespace = "monitoring"

# Contexte minikube local
kube_context    = "minikube"
kubeconfig_path = "~/.kube/config"

app_log_level = "info"