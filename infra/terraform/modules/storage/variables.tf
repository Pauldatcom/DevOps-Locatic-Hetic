variable "app_name" {
  description = "Nom de l'application, utilise pour les labels et le nom du volume."
  type        = string
}

variable "environment" {
  description = "Environnement cible (dev, prod...)."
  type        = string
}

variable "namespace" {
  description = "Namespace Kubernetes dans lequel creer la PVC."
  type        = string
}

variable "storage_class" {
  description = "StorageClass a utiliser. Sur minikube, 'standard' est fourni par defaut."
  type        = string
  default     = "standard"
}

variable "sqlite_size" {
  description = "Taille du volume persistant dedie a SQLite."
  type        = string
  default     = "1Gi"
}

variable "sqlite_access_mode" {
  description = "Mode d'acces a la PVC (ReadWriteOnce pour un seul pod SQLite)."
  type        = string
  default     = "ReadWriteOnce"
}

variable "host_path" {
  description = "Chemin hostPath optionnel pour la PV sur minikube. Si vide, la PV n'est pas creee et on s'appuie sur la StorageClass par defaut."
  type        = string
  default     = ""
}

variable "labels" {
  description = "Labels supplementaires."
  type        = map(string)
  default     = {}
}