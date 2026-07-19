# Template de Secret pour l'application Locatic.
#
# Actuellement, l'application n'utilise pas de secret sensible (pas de base
# de donnees externe, SQLite est un fichier dans un volume). Ce template est
# fourni comme structure : si des variables sensibles doivent etre ajoutees
# plus tard (clé API, mot de passe, token...), les ajouter ici et les valoriser
# via Ansible (lookup dans un vault) ou via `kubectl create secret`.
#
# NE JAMAIS committer de vrai secret dans ce fichier. Toujours utiliser des
# placeholders (ex. CHANGE_ME) et les surcharger au deploiement.
#
# Exemple de creation via Ansible / kubectl :
#   kubectl create secret generic locatic-secret \
#     --from-literal=API_KEY=<valeur> -n <namespace>
apiVersion: v1
kind: Secret
metadata:
  name: locatic-secret
  labels:
    app.kubernetes.io/name: locatic
    app.kubernetes.io/component: app
type: Opaque
stringData:
  # Placeholder - surcharge par Ansible au deploiement.
  APP_SECRET: CHANGE_ME