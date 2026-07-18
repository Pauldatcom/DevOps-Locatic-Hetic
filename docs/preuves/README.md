# Preuves d'execution

Ce dossier regroupe les captures d'ecran, extraits de logs et exports qui
prouvent que les etapes importantes du mini-projet ont ete realisees.

Chaque preuve devrait etre nommee de maniere explicite et referencee dans la
documentation pertinente (`docs/deploiement-local.md`, `docs/exploitation.md`,
`docs/monitoring.md`...).

## Preuves attendues (a completer)

| Etape | Fichier attendu | Statut |
| --- | --- | --- |
| CI verte sur une Pull Request | `ci-pr-green.png` | TODO |
| Image Docker publiee sur ghcr.io | `ghcr-image-published.png` | TODO |
| `terraform apply` reussi sur minikube | `terraform-apply.log` | TODO |
| `terraform output -json` pour Ansible | `terraform-output.json` | TODO |
| `ansible-playbook` execute avec succes | `ansible-playbook.log` | TODO |
| Pods Locatic running sur minikube | `kubectl-pods.png` | TODO |
| PVC SQLite Bound | `kubectl-pvc.png` | TODO |
| Acces a l'application via Nginx | `curl-nginx-home.png` | TODO |
| Endpoint `/health` repond 200 | `curl-health.log` | TODO |
| Endpoint `/metrics` renvoie des metriques Prometheus | `curl-metrics.log` | TODO |
| Donnees SQLite survive a un redemarrage de pod | `sqlite-persistence.log` | TODO |
| Dashboard Grafana affichant l'etat des services | `grafana-dashboard.png` | TODO |
| Targets Prometheus UP | `prometheus-targets.png` | TODO |
| Alertes Prometheus/Alertmanager (bonus) | `alertmanager-alerts.png` | TODO |
| Rollback Helm (bonus) | `helm-rollback.log` | TODO |

## Notes

Les captures et logs sont ajoutes manuellement apres execution des
procedures decrites dans `docs/deploiement-local.md` et `docs/exploitation.md`.
Aucun secret ne doit apparaitre dans ces preuves (tokens, mots de passe,
kubeconfig...).