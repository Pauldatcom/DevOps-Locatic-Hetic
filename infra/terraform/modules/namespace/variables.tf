variable "app_name" {
  description = "Nom de l'application, utilise pour prefixer les ressources."
  type        = string
}

variable "environment" {
  description = "Nom de l'environnement (dev, prod...). Sert a suffixer le namespace applicatif."
  type        = string
}

variable "app_namespace" {
  description = "Nom du namespace Kubernetes dedie a l'application. Si vide, calcule a partir de app_name/environment."
  type        = string
  default     = ""
}

variable "monitoring_namespace" {
  description = "Nom du namespace Kubernetes dedie au monitoring (Prometheus/Grafana)."
  type        = string
  default     = "monitoring"
}

variable "enable_monitoring_namespace" {
  description = "Cree le namespace de monitoring dans cette module. Passer a false si le namespace monitoring est cree ailleurs."
  type        = bool
  default     = true
}

variable "labels" {
  description = "Labels supplementaires appliques aux namespaces."
  type        = map(string)
  default     = {}
}