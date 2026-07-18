.PHONY: help load-image deploy verify verify-strict tf-init tf-apply tf-output \
        ansible-deploy rollback clean

ENV ?= dev
TAG  ?= latest

help:
	@echo "Locatic - orchestration du deploiement local sur minikube"
	@echo ""
	@echo "Cibles disponibles :"
	@echo "  make load-image [TAG=latest]   - pull ghcr.io + minikube image load"
	@echo "  make deploy [ENV=dev]          - terraform apply + ansible-playbook"
	@echo "  make verify [ENV=dev]          - controles post-deploiement (non destructif)"
	@echo "  make verify-strict [ENV=dev]   - idem + test persistance SQLite (redemarre un pod)"
	@echo "  make tf-init [ENV=dev]         - terraform init seulement"
	@echo "  make tf-apply [ENV=dev]        - terraform apply seulement"
	@echo "  make tf-output [ENV=dev]       - ecrit infra/ansible/vars.json depuis terraform output"
	@echo "  make ansible-deploy [ENV=dev]  - ansible-playbook seul (apres tf-output)"
	@echo "  make rollback REV=1 [ENV=dev] - helm rollback (bonus, voir docs/helm.md)"
	@echo "  make clean                    - supprime infra/ansible/vars.json"
	@echo ""
	@echo "Variables : ENV=dev|prod  (default: dev)"
	@echo "            TAG=<sha>|latest  (default: latest)"

load-image:
	./scripts/load-image.sh $(TAG)

tf-init:
	cd infra/terraform/environments/$(ENV) && terraform init

tf-apply:
	cd infra/terraform/environments/$(ENV) && terraform apply -auto-approve

tf-output:
	cd infra/terraform/environments/$(ENV) && terraform output -json ansible_vars > infra/ansible/vars.json
	@echo "Variables Ansible ecrites dans infra/ansible/vars.json :"
	@cat infra/ansible/vars.json

ansible-deploy:
	ansible-playbook infra/ansible/playbook.yml \
		--extra-vars @infra/ansible/vars.json \
		--extra-vars environment=$(ENV)

deploy:
	./scripts/deploy.sh $(ENV)

verify:
	./scripts/verify.sh $(ENV)

verify-strict:
	./scripts/verify.sh $(ENV) --persistence-check

rollback:
	@if [ -z "$(REV)" ]; then echo "Usage: make rollback REV=<revision> ENV=dev"; exit 1; fi
	helm rollback locatic $(REV) -n locatic-$(shell cd infra/terraform/environments/$(ENV) && terraform output -raw app_namespace 2>/dev/null | sed 's/locatic-//')

clean:
	rm -f infra/ansible/vars.json
	@echo "infra/ansible/vars.json supprime"