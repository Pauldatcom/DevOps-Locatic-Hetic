# Mini-projet DevOps WEB2

## Objectif

Mettre en place une chaîne DevOps complète pour une application web : gestion Git professionnelle, pipeline CI/CD GitHub, infrastructure locale automatisée et déploiement Kubernetes sur minikube.

Le résultat attendu est un projet capable de reprendre votre projet de POO Locatic, de passer par une Pull Request validée, d'exécuter automatiquement les contrôles de qualité, de construire une image Docker, puis de déclencher localement le déploiement sur un cluster Kubernetes minikube.

## Contraintes

- Le dépôt GitHub doit être le point central du projet.
- Le point de départ est votre projet de POO Locatic.
- La branche `main` doit être protégée.
- Les changements doivent passer par des Pull Requests.
- Le déploiement final doit se faire sur votre machine avec minikube.
- Aucun VPS ni serveur distant n'est demandé.
- La partie déploiement vers minikube doit être déclenchée depuis votre machine.
- Le pipeline GitHub ne pourra pas déployer directement sur votre minikube local.
- Le pipeline GitHub doit donc s'arrêter après les contrôles, le build, le scan et la publication de l'image.
- Le déploiement local doit être déclenché par Terraform puis Ansible.
- L'architecture déployée doit contenir Nginx en reverse proxy devant l'application.
- L'application doit être derrière Nginx et ne doit pas être exposée directement comme point d'entrée principal.
- Aucune base de données externe n'est demandée.
- L'application utilise SQLite avec un volume de stockage persistant.
- Chaque service déployé doit être monitoré avec Prometheus et visualisé dans Grafana.
- Les secrets, mots de passe, tokens et fichiers d'état Terraform ne doivent pas être versionnés.
- Les fichiers nécessaires à la CI, à l'infrastructure et au déploiement doivent être présents dans le dépôt respectant la structure vu en cours..

## Schéma d'architecture attendu

![Schéma d'architecture attendu](assets/mini-project-architecture.png)

Le schéma représente la cible attendue :

- GitHub sert à gérer le code, les Pull Requests, les checks et la publication de l'image.
- GitHub Actions ne déploie pas sur minikube.
- Le déploiement sur minikube est déclenché depuis votre machine.
- Terraform prépare l'infrastructure locale.
- Ansible orchestre la suite du déploiement.
- Le déploiement Kubernetes installe ou met à jour Nginx, l'application, le stockage SQLite et le monitoring.
- Helm est un bonus possible, pas une obligation.
- Nginx est le point d'entrée utilisateur.
- L'application est derrière Nginx.
- SQLite utilise un volume persistant.
- Prometheus collecte les métriques.
- Grafana affiche l'état des services.

## Étapes attendues

### 1. Reprendre votre projet de POO

Reprendre le projet Locatic réalisé en POO.

Le projet de départ attendu est l'application Locatic : une application ASP.NET Core MVC de location de voitures, avec persistance SQLite, gestion des marques, modèles, voitures, clients et réservations.

Votre travail DevOps doit partir de ce projet existant. Vous pouvez corriger, stabiliser ou adapter le projet si nécessaire, mais vous devez conserver l'application métier comme base du déploiement.

Le dépôt final doit permettre de voir clairement :

- le code applicatif issu de votre projet de POO
- les adaptations nécessaires au conteneur Docker
- les fichiers DevOps ajoutés pour la CI, l'infrastructure, minikube et le monitoring
- les éventuelles corrections réalisées pour rendre le projet exploitable en déploiement

### 2. Préparer le dépôt GitHub

Créer ou reprendre le dépôt GitHub de votre projet de POO.

Le dépôt doit contenir au minimum :

- le code source de l'application ASP.NET Core MVC
- les tests automatisés disponibles
- un Dockerfile
- les fichiers de configuration GitHub Actions
- un dossier d'infrastructure
- des fichiers de déploiement Kubernetes
- un chart Helm uniquement si vous réalisez le bonus
- une configuration de stockage pour SQLite
- une configuration de monitoring Prometheus et Grafana
- une documentation d'exécution locale

Configurer une organisation claire des dossiers afin de séparer l'application, la CI, l'infrastructure et le déploiement.

### 3. Mettre en place les bonnes pratiques GitHub

Configurer le dépôt pour imposer un travail par Pull Request.

La branche `main` doit être protégée avec au minimum :

- interdiction des push directs
- validation obligatoire d'une Pull Request
- checks CI obligatoires avant merge
- historique lisible des changements

Créer une première Pull Request qui montre le fonctionnement réel de cette organisation.

### 4. Préparer l'application pour le déploiement

Adapter l'application pour qu'elle puisse tourner dans un conteneur.

L'application doit proposer :

- un lancement reproductible
- une configuration par variables d'environnement
- un endpoint ou mécanisme de vérification de santé
- des tests exécutables automatiquement
- une image Docker construisible localement et en CI
- une configuration SQLite compatible avec un volume persistant

L'application ne doit pas dépendre d'une base de données externe. Les données persistantes doivent être stockées dans SQLite, avec un chemin configurable pour permettre le montage d'un volume dans Kubernetes.

Le Dockerfile doit être adapté à un usage DevOps : image raisonnablement légère, build reproductible, utilisateur non privilégié si possible, dépendances maîtrisées.

### 5. Créer le pipeline CI avec GitHub Actions

Créer un workflow GitHub Actions qui s'exécute automatiquement sur les Pull Requests et sur la branche `main`.

Le pipeline doit inclure :

- récupération du code
- installation des dépendances
- exécution des tests
- contrôle de qualité ou lint si disponible
- build de l'image Docker
- scan de sécurité de l'image ou du dépôt
- publication de l'image dans une registry uniquement quand le code est intégré à `main`

Les jobs doivent être organisés de manière lisible, avec des dépendances explicites entre les étapes.

Le pipeline doit échouer si une étape importante échoue.

Il est normal que le pipeline GitHub ne puisse pas aller jusqu'au bout du déploiement final, car minikube tourne sur votre machine et non sur les runners GitHub.

Le pipeline GitHub ne doit pas essayer d'appliquer Terraform, d'exécuter Ansible ou de déployer sur minikube. Ces opérations ciblent votre machine locale et doivent être lancées localement après la publication de l'image.

### 6. Gérer les secrets et la registry

Configurer la publication de l'image Docker dans une registry compatible avec GitHub.

Les informations sensibles doivent être stockées dans les secrets GitHub ou dans des fichiers locaux ignorés par Git.

Le dépôt ne doit pas contenir :

- token personnel
- mot de passe
- fichier `.env` réel
- fichier `terraform.tfstate`
- clé privée
- secret Kubernetes en clair destiné à un usage réel

### 7. Préparer le cluster local avec minikube

Installer et lancer un cluster Kubernetes local avec minikube.

Le cluster doit pouvoir :

- recevoir l'image de l'application
- exposer l'application localement
- redémarrer l'application en cas d'échec
- appliquer une configuration différente selon l'environnement choisi
- exécuter une stack de monitoring locale

Le déploiement doit être vérifiable depuis le poste de travail.

### 8. Automatiser l'infrastructure avec Terraform

Créer une configuration Terraform pour préparer les éléments d'infrastructure nécessaires au déploiement local.

Terraform doit gérer ou préparer au minimum :

- le namespace Kubernetes de l'application
- le stockage persistant nécessaire à SQLite
- les variables ou sorties nécessaires aux étapes suivantes
- la séparation entre configuration locale et fichiers versionnés
- la gestion propre de l'état Terraform

L'état Terraform ne doit pas être commité.

Le projet doit expliquer comment initialiser, planifier et appliquer cette infrastructure depuis une machine locale.

Après l'application de Terraform, les informations nécessaires au déploiement doivent permettre à Ansible de poursuivre l'installation.

### 9. Automatiser l'orchestration locale avec Ansible

Créer un playbook Ansible exécuté depuis votre machine.

Le playbook doit servir à automatiser les actions locales liées au déploiement, par exemple :

- vérifier les prérequis
- vérifier que minikube est disponible
- récupérer les informations produites par Terraform
- préparer les valeurs nécessaires au déploiement
- appliquer ou mettre à jour les ressources Kubernetes locales

Ansible doit être utilisé comme outil d'orchestration entre l'infrastructure préparée et le déploiement applicatif.

Le playbook doit déclencher le déploiement complet attendu : Nginx en reverse proxy, l'application derrière Nginx, le volume persistant utilisé par SQLite, et la stack de monitoring.

Si vous réalisez le bonus Helm, Ansible peut aussi lancer ou mettre à jour une release Helm.

### 10. Préparer le déploiement Kubernetes

Créer les fichiers nécessaires au déploiement Kubernetes de l'application.

Le déploiement doit contenir les ressources Kubernetes nécessaires :

- Deployment pour l'application
- Deployment ou configuration équivalente pour Nginx
- Service
- ConfigMap si nécessaire
- Secret sous forme de template si nécessaire
- volume persistant pour SQLite
- probes de santé
- ressources CPU et mémoire
- configuration de l'image et du tag
- configuration nécessaire pour exposer les métriques au monitoring

La configuration doit permettre de changer facilement :

- le nom de l'image
- le tag de l'image
- le nombre de replicas
- les variables d'environnement
- le type d'exposition du service
- le chemin de stockage SQLite
- la configuration Nginx nécessaire au reverse proxy
- l'activation ou la configuration du monitoring

### 11. Déployer sur minikube

Réaliser le déploiement complet sur le cluster minikube local.

Le chemin attendu est :

1. le projet de POO Locatic est repris comme base du dépôt
2. le code passe par une Pull Request
3. la CI valide la Pull Request
4. le merge vers `main` publie une image Docker
5. le pipeline GitHub s'arrête après la publication de l'image
6. Terraform prépare l'infrastructure locale sur minikube
7. Ansible orchestre le déploiement depuis votre machine
8. les ressources Kubernetes installent ou mettent à jour Nginx, l'application et le stockage SQLite
9. Prometheus et Grafana supervisent les services déployés
10. minikube exécute l'ensemble

Le point d'entrée utilisateur doit passer par Nginx. L'application doit être accessible derrière ce reverse proxy et utiliser son volume SQLite pour conserver ses données.

Le résultat doit être observable avec les outils Kubernetes habituels et accessible depuis le poste local.

### 12. Ajouter le monitoring avec Prometheus et Grafana

Ajouter une stack de monitoring locale intégrée au déploiement.

Le monitoring doit inclure :

- Prometheus
- Grafana
- collecte des métriques de l'application
- collecte des métriques de Nginx ou indicateurs équivalents
- visualisation de l'état des pods et services Kubernetes
- visualisation de l'état du stockage utilisé par SQLite
- dashboard permettant de comprendre rapidement l'état de chaque service
- alertes simples sur les problèmes importants

Chaque service important doit avoir au moins un indicateur visible dans Grafana. Le dashboard doit permettre d'identifier si Nginx, l'application, le stockage et les composants de monitoring fonctionnent correctement.

### 13. Documenter le projet

Le dépôt doit contenir une documentation claire permettant à une autre personne de comprendre et d'exécuter le projet.

La documentation doit couvrir :

- l'objectif du projet
- le lien avec votre projet de POO de départ
- les prérequis locaux
- la structure du dépôt
- le fonctionnement des Pull Requests
- le fonctionnement du pipeline CI/CD
- la gestion des secrets
- les étapes de déploiement local
- le fonctionnement du monitoring Prometheus et Grafana
- la procédure de vérification après déploiement
- les limites connues

La documentation ne doit pas supposer que le correcteur connaît votre machine.

## Parcours de réalisation conseillé

Les étapes ci-dessous donnent un ordre de travail attendu. Elles ne remplacent pas votre réflexion technique et ne donnent pas les fichiers finaux à produire.

1. Vérifier que le projet de POO Locatic tourne encore en local.
2. Identifier le fichier SQLite, son chemin actuel et les dépendances nécessaires au lancement.
3. Ajouter ou stabiliser les tests automatisés existants.
4. Ajouter une configuration de santé permettant de vérifier que l'application répond correctement.
5. Conteneuriser l'application et vérifier que le conteneur fonctionne localement.
6. Préparer la publication de l'image dans une registry.
7. Mettre en place le workflow GitHub Actions pour les Pull Requests.
8. Ajouter le build, les tests, le scan et la publication de l'image sur `main`.
9. Protéger la branche `main` avec des checks obligatoires.
10. Créer l'infrastructure Terraform locale pour minikube.
11. Préparer les outputs Terraform utiles au déploiement.
12. Créer le playbook Ansible qui enchaîne les vérifications locales et le déploiement.
13. Créer les fichiers Kubernetes de l'application.
14. Ajouter Nginx dans le déploiement comme reverse proxy.
15. Ajouter le volume persistant utilisé par SQLite.
16. Déployer l'ensemble sur minikube depuis votre poste.
17. Ajouter Prometheus et Grafana.
18. Ajouter les métriques ou indicateurs nécessaires pour chaque service important.
19. Vérifier l'accès utilisateur via Nginx.
20. Vérifier que les données SQLite survivent à un redémarrage de pod.
21. Vérifier que Grafana permet de comprendre l'état de Nginx, de l'application, du stockage et du monitoring.
22. Documenter les choix principaux et conserver des preuves des étapes importantes.

## Documentation attendue

Le dépôt doit contenir une documentation exploitable par une autre personne. Les noms exacts peuvent varier, mais les contenus suivants doivent exister.

- `README.md` : objectif, prérequis, structure du dépôt, démarrage rapide et lien vers les autres docs.
- `docs/architecture.md` : explication de l'architecture, rôle de GitHub Actions, rôle de Terraform, rôle d'Ansible, rôle du déploiement Kubernetes, rôle de Nginx, rôle du volume SQLite et rôle du monitoring.
- `docs/ci-cd.md` : règles de branche, Pull Requests, checks obligatoires, jobs du pipeline, publication de l'image et limites du pipeline GitHub.
- `docs/deploiement-local.md` : ordre exact des actions locales pour passer d'une image publiée à une application déployée sur minikube.
- `docs/terraform.md` : ressources gérées, variables attendues, outputs utiles et gestion de l'état.
- `docs/ansible.md` : rôle du playbook, étapes orchestrées et dépendance aux outputs Terraform.
- `docs/kubernetes.md` : ressources Kubernetes utilisées, services exposés, stockage SQLite et configuration Nginx.
- `docs/helm.md` : structure du chart, valeurs configurables et procédure de release si vous réalisez le bonus Helm.
- `docs/monitoring.md` : services monitorés, métriques suivies, accès à Prometheus, accès à Grafana et lecture du dashboard.
- `docs/exploitation.md` : vérifications après déploiement, logs utiles, rollback, problèmes fréquents et commandes de diagnostic.
- `docs/preuves/` : captures, extraits de logs ou exports montrant les étapes importantes.

La documentation doit expliquer vos choix. Elle ne doit pas seulement contenir une liste de commandes.

## Aide commandes

Les commandes ci-dessous sont une aide pour avancer et vérifier votre travail. Elles ne sont pas toutes obligatoires et ne sont pas à recopier telles quelles. Les valeurs entre chevrons doivent être adaptées à votre projet.

### Git et GitHub

- `git status` : vérifier les fichiers modifiés, ajoutés ou non suivis avant de créer un commit.
- `git checkout -b <nom-branche>` : créer une branche de travail dédiée à une modification.
- `git add <fichiers>` : sélectionner les fichiers qui feront partie du prochain commit.
- `git commit -m "<message>"` : enregistrer une étape de travail avec un message compréhensible.
- `git push <remote> <branche>` : envoyer la branche sur GitHub pour ouvrir une Pull Request.
- `gh pr create` ou création de Pull Request depuis l'interface GitHub : proposer la modification à la revue avant merge.
- vérification des règles de protection de `main` : confirmer que les merges passent bien par Pull Request et checks obligatoires.

### Application ASP.NET Core

- `dotnet restore` : télécharger les dépendances nécessaires au projet.
- `dotnet build` : vérifier que l'application compile sans erreur.
- `dotnet test` : exécuter les tests automatisés disponibles.
- `dotnet run` : lancer l'application localement pour vérifier son comportement avant conteneurisation.
- commande de migration ou d'initialisation SQLite si votre projet en a besoin : préparer la base locale ou appliquer le schéma attendu.
- commande permettant de vérifier l'endpoint de santé : confirmer que l'application expose un point de contrôle exploitable par Kubernetes.

### Docker et registry

- `docker build -t <image>:<tag> <contexte>` : construire l'image de l'application à partir du Dockerfile.
- `docker run <options> <image>:<tag>` : lancer l'image localement pour vérifier qu'elle démarre correctement.
- `docker logs <conteneur>` : consulter les logs du conteneur en cas de problème de démarrage ou d'exécution.
- `docker push <image>:<tag>` : publier l'image dans la registry utilisée par le déploiement.
- commande de vérification de l'image publiée dans la registry : confirmer que le tag attendu existe et peut être récupéré.

### Sécurité et qualité

- commande de scan d'image avec l'outil choisi : détecter des vulnérabilités connues dans l'image Docker.
- commande de scan de secrets avec l'outil choisi : vérifier qu'aucun token, mot de passe ou secret n'est présent dans le dépôt.
- commande de lint ou de format si votre projet en utilise une : contrôler la cohérence du code avant merge.

### minikube et Kubernetes

- `minikube start` : démarrer le cluster Kubernetes local.
- `minikube status` : vérifier que le cluster local est bien actif.
- `kubectl get namespaces` : contrôler que les namespaces attendus existent.
- `kubectl get all -n <namespace>` : afficher les ressources déployées dans le namespace du projet.
- `kubectl describe <ressource> -n <namespace>` : inspecter une ressource Kubernetes pour comprendre son état ou ses erreurs.
- `kubectl logs <pod> -n <namespace>` : lire les logs d'un pod pour diagnostiquer l'application ou un service.
- `kubectl port-forward <ressource> <port-local>:<port-service> -n <namespace>` : exposer temporairement un service Kubernetes sur votre machine.
- commande permettant de vérifier que l'accès passe par Nginx : confirmer que le point d'entrée utilisateur est le reverse proxy et non l'application directement.

### Terraform

- `terraform init` : initialiser le dossier Terraform et ses providers.
- `terraform fmt` : reformater les fichiers Terraform de manière standard.
- `terraform validate` : vérifier que la configuration Terraform est syntaxiquement valide.
- `terraform plan` : prévisualiser les changements avant application.
- `terraform apply` : appliquer les changements d'infrastructure locale.
- `terraform output` : récupérer les valeurs utiles pour Ansible ou la suite du déploiement.
- vérification que l'état Terraform n'est pas versionné : s'assurer que les fichiers d'état ne sont pas présents dans Git.

### Ansible

- `ansible --version` : vérifier qu'Ansible est installé sur le poste local.
- `ansible-playbook <playbook>.yml --check` : simuler l'exécution du playbook quand les modules utilisés le permettent.
- `ansible-playbook <playbook>.yml` : exécuter l'orchestration locale du déploiement.
- commande de vérification des variables utilisées par le playbook : confirmer que les valeurs nécessaires sont bien disponibles avant exécution.

### Helm bonus

- `helm lint <chart>` : vérifier la cohérence du chart Helm.
- `helm template <release> <chart>` : générer les manifests Kubernetes sans les appliquer pour les relire.
- `helm upgrade --install <release> <chart>` : installer ou mettre à jour la release Helm.
- `helm status <release>` : vérifier l'état courant de la release.
- `helm history <release>` : consulter les versions précédentes de la release.
- `helm rollback <release> <revision>` : revenir à une version précédente si vous avez mis en place le bonus Helm.

### Monitoring

- commande d'accès local à Prometheus : ouvrir l'interface Prometheus depuis votre poste pour vérifier la collecte.
- commande d'accès local à Grafana : ouvrir Grafana depuis votre poste pour consulter les dashboards.
- commande de vérification que Prometheus collecte les targets attendues : contrôler que Nginx, l'application et les services importants sont bien visibles.
- commande ou capture montrant que Grafana affiche chaque service important : prouver que le dashboard permet de lire l'état de l'architecture déployée.

## Livrables

- lien vers le dépôt GitHub
- preuve que le projet part de votre projet de POO
- Pull Request représentative du workflow de travail
- pipeline GitHub Actions fonctionnel
- image Docker publiée dans une registry
- configuration Terraform
- playbook Ansible
- fichiers Kubernetes de déploiement
- chart Helm si vous réalisez le bonus
- application déployée sur minikube derrière Nginx
- volume persistant utilisé par SQLite
- monitoring Prometheus et Grafana fonctionnel
- dashboard Grafana couvrant les services déployés
- documentation d'architecture avec le schéma fourni ou un schéma équivalent
- documentation d'installation et d'exploitation
- captures ou preuves d'exécution des étapes principales

## Critères d'évaluation

| Critère | Points |
| --- | ---: |
| Reprise réelle du projet de POO et qualité de la documentation | 2 |
| Bonnes pratiques GitHub, Pull Requests et branche `main` protégée | 3 |
| Pipeline CI GitHub Actions fonctionnel | 3 |
| Build, scan et publication de l'image Docker | 2 |
| Terraform utilisé proprement pour l'infrastructure locale | 2 |
| Ansible utilisé pour orchestrer le déploiement local | 2 |
| Déploiement Kubernetes configurable avec Nginx, application et stockage SQLite | 2 |
| Déploiement fonctionnel sur minikube derrière Nginx | 2 |
| Monitoring Prometheus et Grafana de chaque service | 2 |
| **Total** | **20** |

## Bonus

| Bonus | Points |
| --- | ---: |
| Alertes pertinentes et testées | +1 |
| Dashboard clair pour comparer l'état des services | +1 |
| Chart Helm configurable et utilisé par Ansible | +2 |
| Procédure de rollback documentée et démontrée | +1 |
| Pipeline clair, rapide et bien séparé en jobs | +1 |

## Points de vigilance
- Un pipeline qui passe au vert sans tester réellement l'application ne suffit pas.
- Un déploiement fait uniquement à la main sans Terraform ni Ansible ne suffit pas.
- Une image construite localement mais jamais publiée ne respecte pas l'objectif CI/CD.
- Une application exposée directement sans passer par Nginx ne respecte pas l'architecture demandée.
- Une persistance SQLite non montée sur un volume ne respecte pas les consignes.
- Un monitoring limité à un seul service ne respecte pas les consignes.
- Un dashboard Grafana sans données Prometheus exploitables ne suffit pas.
- Un secret présent dans Git est une erreur critique.
- Un fichier d'état Terraform présent dans Git est une erreur critique.
