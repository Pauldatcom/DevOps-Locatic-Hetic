#!/usr/bin/env bash
# Install prerequisites on Linux/macOS (or WSL) for Locatic DevOps.
set -euo pipefail

echo "==> Checking Docker"
docker version >/dev/null

OS="$(uname -s)"
case "$OS" in
  Linux)
    if ! command -v minikube >/dev/null 2>&1; then
      echo "==> Installing minikube"
      curl -fsSL -o /tmp/minikube https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
      sudo install /tmp/minikube /usr/local/bin/minikube
    fi
    if ! command -v terraform >/dev/null 2>&1; then
      echo "==> Installing Terraform via apt (HashiCorp repo may be required)"
      sudo apt-get update -qq
      sudo apt-get install -y -qq terraform || {
        echo "Install Terraform manually: https://developer.hashicorp.com/terraform/install"
      }
    fi
    sudo apt-get install -y -qq ansible python3-pip python3-kubernetes || true
    ;;
  Darwin)
    if command -v brew >/dev/null 2>&1; then
      brew list minikube >/dev/null 2>&1 || brew install minikube
      brew list terraform >/dev/null 2>&1 || brew install terraform
      brew list ansible >/dev/null 2>&1 || brew install ansible
    else
      echo "Install Homebrew, then: brew install minikube terraform ansible"
      exit 1
    fi
    ;;
esac

pip3 install --user kubernetes >/dev/null 2>&1 || true
ansible-galaxy collection install kubernetes.core -f >/dev/null

echo "==> Versions"
minikube version
terraform version
kubectl version --client
ansible --version | head -n 1

echo "Prereqs ready. Next: ./scripts/deploy.sh dev minikube"
