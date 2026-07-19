output "pvc_name" {
  description = "Nom de la PersistentVolumeClaim utilisee par SQLite."
  value       = kubernetes_persistent_volume_claim.sqlite.metadata[0].name
}

output "pvc_namespace" {
  description = "Namespace de la PVC SQLite."
  value       = kubernetes_persistent_volume_claim.sqlite.metadata[0].namespace
}

output "pvc_size" {
  description = "Taille demandee pour la PVC SQLite."
  value       = var.sqlite_size
}

output "storage_class" {
  description = "StorageClass utilisee."
  value       = var.storage_class
}

output "mount_path" {
  description = "Chemin de montage du volume dans le conteneur applicatif."
  value       = "/data"
}