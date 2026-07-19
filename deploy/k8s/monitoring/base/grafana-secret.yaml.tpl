# Template de Secret Grafana.
#
# NE JAMAIS committer de vrai mot de passe.
# Ansible cree le Secret `grafana-admin-secret` au deploiement
# (voir infra/ansible/roles/k8s_deploy/tasks/main.yml).
#
# Exemple manuel :
#   kubectl -n monitoring create secret generic grafana-admin-secret \
#     --from-literal=GF_SECURITY_ADMIN_USER=admin \
#     --from-literal=GF_SECURITY_ADMIN_PASSWORD='<mot-de-passe>'
apiVersion: v1
kind: Secret
metadata:
  name: grafana-admin-secret
  labels:
    app.kubernetes.io/name: grafana
    app.kubernetes.io/component: monitoring
type: Opaque
stringData:
  GF_SECURITY_ADMIN_USER: CHANGE_ME
  GF_SECURITY_ADMIN_PASSWORD: CHANGE_ME
