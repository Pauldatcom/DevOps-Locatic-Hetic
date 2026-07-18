output "app_namespace" {
  description = "Nom du namespace Kubernetes de l'application."
  value       = local.resolved_app_namespace
}

output "monitoring_namespace" {
  description = "Nom du namespace Kubernetes de monitoring."
  value       = var.enable_monitoring_namespace ? kubernetes_namespace.monitoring[0].metadata[0].name : var.monitoring_namespace
}

output "app_namespace_resource" {
  description = "Objet namespace applicatif complet (pour reference Ansible)."
  value = {
    name   = kubernetes_namespace.app.metadata[0].name
    labels = kubernetes_namespace.app.metadata[0].labels
  }
}