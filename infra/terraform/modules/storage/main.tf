locals {
  pvc_name = "${var.app_name}-sqlite"
  pv_name  = "${var.app_name}-sqlite"

  common_labels = merge({
    "app.kubernetes.io/name"        = var.app_name
    "app.kubernetes.io/environment" = var.environment
    "app.kubernetes.io/component"   = "storage"
    "managed-by"                    = "terraform"
  }, var.labels)
}

# La PV n'est creee que si un hostPath explicite est fourni. Sur minikube, la
# StorageClass 'standard' provisionne dynamiquement une PV ; dans ce cas on
# laisse la PVC declencher le provisioning.
resource "kubernetes_persistent_volume" "sqlite" {
  count = var.host_path != "" ? 1 : 0

  metadata {
    name   = local.pv_name
    labels = local.common_labels
  }

  spec {
    capacity = {
      storage = var.sqlite_size
    }
    access_modes                     = [var.sqlite_access_mode]
    storage_class_name               = var.storage_class
    persistent_volume_reclaim_policy = "Retain"

    persistent_volume_source {
      host_path {
        path = var.host_path
        type = "DirectoryOrCreate"
      }
    }
  }
}

resource "kubernetes_persistent_volume_claim" "sqlite" {
  metadata {
    name      = local.pvc_name
    namespace = var.namespace
    labels    = local.common_labels
  }

  spec {
    access_modes       = [var.sqlite_access_mode]
    storage_class_name = var.storage_class
    resources {
      requests = {
        storage = var.sqlite_size
      }
    }
  }
}