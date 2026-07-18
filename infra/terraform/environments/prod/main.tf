terraform {
  required_version = ">= 1.6"

  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.35"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 3.0"
    }
  }
}

provider "kubernetes" {
  config_path    = var.kubeconfig_path
  config_context = var.kube_context
}

provider "helm" {
  kubernetes {
    config_path    = var.kubeconfig_path
    config_context = var.kube_context
  }
}

# ---------------------------------------------------------------------------
# Variables
# ---------------------------------------------------------------------------

variable "app_name" {
  description = "Nom de l'application (Locatic)."
  type        = string
}

variable "environment" {
  description = "Nom de l'environnement de production."
  type        = string
}

variable "kubeconfig_path" {
  description = "Chemin vers le kubeconfig utilise par minikube."
  type        = string
  default     = "~/.kube/config"
}

variable "kube_context" {
  description = "Contexte Kubernetes a utiliser (typiquement 'minikube')."
  type        = string
  default     = "minikube"
}

variable "monitoring_namespace" {
  description = "Namespace dedie au monitoring Prometheus/Grafana."
  type        = string
  default     = "monitoring"
}

variable "app_image" {
  description = "Image Docker de l'application publiee par la CI (ghcr.io/.../locatic)."
  type        = string
}

variable "app_tag" {
  description = "Tag de l'image a deployer. En prod on epingle un SHA precis publie par la CI."
  type        = string
}

variable "app_replicas" {
  description = "Nombre de replicas du Deployment applicatif."
  type        = number
  default     = 3
}

variable "sqlite_size" {
  description = "Taille du volume persistant SQLite."
  type        = string
  default     = "2Gi"
}

variable "sqlite_host_path" {
  description = "hostPath optionnel pour le PV SQLite sur le noeud minikube. Laisser vide pour utiliser la StorageClass 'standard' de minikube."
  type        = string
  default     = ""
}

variable "app_log_level" {
  description = "Niveau de log applicatif transmis via variable d'environnement."
  type        = string
  default     = "info"
}

# ---------------------------------------------------------------------------
# Modules
# ---------------------------------------------------------------------------

module "namespace" {
  source                      = "../../modules/namespace"
  app_name                    = var.app_name
  environment                 = var.environment
  monitoring_namespace        = var.monitoring_namespace
  enable_monitoring_namespace = true
}

module "storage" {
  source      = "../../modules/storage"
  app_name    = var.app_name
  environment = var.environment
  namespace   = module.namespace.app_namespace
  sqlite_size = var.sqlite_size
  host_path   = var.sqlite_host_path
}

# ---------------------------------------------------------------------------
# Outputs (consommes par Ansible via `terraform output -json`)
# ---------------------------------------------------------------------------

output "app_namespace" {
  description = "Namespace de l'application deployee."
  value       = module.namespace.app_namespace
}

output "monitoring_namespace" {
  description = "Namespace de la stack de monitoring."
  value       = module.namespace.monitoring_namespace
}

output "sqlite_pvc_name" {
  description = "Nom de la PVC montee sur /data par le pod applicatif."
  value       = module.storage.pvc_name
}

output "sqlite_mount_path" {
  description = "Chemin de montage du volume SQLite dans le conteneur."
  value       = module.storage.mount_path
}

output "app_image" {
  description = "Image Docker deployee."
  value       = "${var.app_image}:${var.app_tag}"
}

output "app_replicas" {
  description = "Nombre de replicas demandes."
  value       = var.app_replicas
}

output "app_log_level" {
  description = "Niveau de log transmis a l'application."
  value       = var.app_log_level
}

output "kube_context" {
  description = "Contexte Kubernetes utilise pour le deploiement."
  value       = var.kube_context
}

output "ansible_vars" {
  description = "Variables synthese pour Ansible (terraform output -json)."
  value = {
    app_namespace        = module.namespace.app_namespace
    monitoring_namespace = module.namespace.monitoring_namespace
    sqlite_pvc_name      = module.storage.pvc_name
    sqlite_mount_path    = module.storage.mount_path
    app_image            = "${var.app_image}:${var.app_tag}"
    app_replicas         = var.app_replicas
    app_log_level        = var.app_log_level
    kube_context         = var.kube_context
  }
}