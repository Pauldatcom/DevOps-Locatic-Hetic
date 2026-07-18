#!/usr/bin/env bash
# Charge l'image Docker Locatic publiee sur ghcr.io dans le cluster minikube.
#
# Sur minikube, le daemon Docker du cluster est isole du daemon Docker de
# l'hote. Pour qu'un pod puisse utiliser une image sans la tirer depuis ghcr.io
# (et sans imagePullSecrets), on la charge explicitement dans minikube via
# `minikube image load`. C'est l'approche documentee dans
# `docs/deploiement-local.md`.
#
# Usage:
#   ./scripts/load-image.sh [tag]
#
# Par defaut le tag est 'latest'. Pour epingler un SHA publie par la CI:
#   ./scripts/load-image.sh a1b2c3d

set -euo pipefail

IMAGE_REPO="ghcr.io/pauldatcom/locatic"
TAG="${1:-latest}"
REMOTE_IMAGE="${IMAGE_REPO}:${TAG}"
LOCAL_IMAGE="locatic:${TAG}"

log() { printf '\033[1;34m[load-image]\033[0m %s\n' "$*"; }
err()  { printf '\033[1;31m[load-image]\033[0m %s\n' "$*" >&2; }

# Preconditions
command -v docker   >/dev/null 2>&1 || { err "docker introuvable"; exit 1; }
command -v minikube >/dev/null 2>&1 || { err "minikube introuvable"; exit 1; }

minikube status >/dev/null 2>&1 || { err "minikube n'est pas demarre. Lancez 'minikube start'."; exit 1; }

log "Pull de l'image depuis ghcr.io : ${REMOTE_IMAGE}"
docker pull "${REMOTE_IMAGE}"

log "Re-tag local : ${REMOTE_IMAGE} -> ${LOCAL_IMAGE}"
docker tag "${REMOTE_IMAGE}" "${LOCAL_IMAGE}"

log "Chargement dans minikube (peut prendre 30-60s)..."
minikube image load "${LOCAL_IMAGE}"

log "Verification : l'image est visible dans minikube"
minikube image ls | grep -E "^${LOCAL_IMAGE}\$" \
  || { err "image non trouvee dans minikube"; exit 1; }

log "Image chargee avec succes : ${LOCAL_IMAGE}"
log "Vous pouvez maintenant lancer : ./scripts/deploy.sh dev"