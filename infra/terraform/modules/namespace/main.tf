locals {
  resolved_app_namespace = var.app_namespace != "" ? var.app_namespace : "${var.app_name}-${var.environment}"

  common_labels = merge({
    "app.kubernetes.io/name"        = var.app_name
    "app.kubernetes.io/environment" = var.environment
    "managed-by"                    = "terraform"
  }, var.labels)
}

resource "kubernetes_namespace" "app" {
  metadata {
    name   = local.resolved_app_namespace
    labels = local.common_labels
  }
}

resource "kubernetes_namespace" "monitoring" {
  count = var.enable_monitoring_namespace ? 1 : 0
  metadata {
    name = var.monitoring_namespace
    labels = merge(local.common_labels, {
      "app.kubernetes.io/component" = "monitoring"
    })
  }
}