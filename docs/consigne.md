# Locatic — Agence de location de voitures

Tu construis une application web **ASP.NET Core MVC** adossée à une base de données **SQLite** (via Entity Framework Core) pour gérer une petite agence de location de voitures : son catalogue, ses clients et ses réservations.

Projet à rendre par git, lien à envoyer par mail à sean@pandor.media (Objet [WEB2POO] - Membre du binome Membre du binome)

## Contexte

Une agence loue des voitures. Chaque voiture appartient à un **modèle** (ex. Clio, 208, Série 3), et chaque modèle est produit par une **marque** (ex. Renault, Peugeot, BMW). L'agence enregistre ses **clients** et les **réservations** qu'ils posent sur les voitures.

L'objectif : disposer d'un site où l'on peut tenir à jour le parc de voitures, la liste des clients, et les réservations, le tout **persisté dans une base SQLite** qui survit au redémarrage du serveur.

## Le domaine à modéliser

Voici les entités attendues et la façon dont elles s'articulent. Les attributs listés sont des **pistes** : à toi de fixer les propriétés exactes et leurs types.

- **Marque** — un constructeur automobile. Au minimum : un nom. (éventuellement : pays d'origine).
- **Modèle** — un modèle de voiture, rattaché à **une** marque. Au minimum : un nom, et le lien vers sa marque.
- **Voiture** — un véhicule concret du parc, d'**un** modèle donné. Par exemple : immatriculation, année, tarif journalier, nombre de places, type de carburant, et le lien vers son modèle.
- **Client** — une personne qui loue. Par exemple : nom, prénom, email, téléphone. (Reste simple.)
- **Réservation** — la location d'**une** voiture par **un** client sur une période. Par exemple : date de début, date de fin, et les liens vers la voiture et le client.

### Relations attendues

- Une **Marque** regroupe plusieurs **Modèles** ; un Modèle appartient à une seule Marque.
- Un **Modèle** se décline en plusieurs **Voitures** ; une Voiture est d'un seul Modèle.
- Un **Client** peut avoir plusieurs **Réservations** ; une Réservation concerne un seul Client.
- Une **Voiture** peut figurer dans plusieurs **Réservations** (à des périodes différentes) ; une Réservation concerne une seule Voiture.

> Conséquence directe : depuis une voiture, on doit pouvoir remonter à son modèle **puis** à sa marque. Réfléchis bien à ces chaînes de relations avant d'écrire la moindre vue — c'est le cœur du projet.

## Fonctionnalités attendues

### Marques & modèles

- Lister les marques et les modèles existants.
- Ajouter une marque.
- Ajouter un modèle **en le rattachant à une marque existante** (l'utilisateur choisit la marque, il ne la ressaisit pas).

### Voitures

- **CRUD complet** : lister, consulter le détail, ajouter, modifier, supprimer une voiture.
- À l'ajout / la modification, on **choisit le modèle** de la voiture (et donc, indirectement, sa marque).
- L'affichage d'une voiture fait apparaître **sa marque et son modèle**, pas seulement un identifiant.

### Clients

- Créer et lister les clients. (CRUD léger suffit — reste simple.)

### Réservations

- Créer une réservation en associant **un client** et **une voiture** sur une période (date de début / date de fin).
- Lister les réservations, de façon lisible (qui, quelle voiture, quelles dates).
- Implémenter **au moins une règle métier** de cohérence, par exemple : refuser une date de fin antérieure à la date de début, ou signaler qu'une voiture est déjà réservée sur la période demandée.

### Persistance

- Toutes les données (marques, modèles, voitures, clients, réservations) vivent dans une **base SQLite** pilotée par **EF Core**.
- Les données **survivent au redémarrage** de l'application.

## Contraintes techniques

- Application **ASP.NET Core MVC**.
- Persistance en **SQLite** avec **Entity Framework Core** (un `DbContext`, des migrations).
- **Architecture en couches** : le domaine, l'accès aux données, la logique applicative (services), les controllers, les vues. Aucun accès direct à la base ni logique métier dans les controllers — ils orchestrent, c'est tout.
- **Injection de dépendances** pour le `DbContext` et tes services / repositories.
- **Validation côté serveur** des formulaires (DataAnnotations + `ModelState`).
- Vues **Razor**. Libre à toi de soigner la présentation (Tailwind ou Bootstrap).

## Conseils

- Commence par le **modèle de données et ses relations**, fais-le tourner en base, puis seulement après attaque les vues.
- Réfléchis aux **propriétés de navigation** entre entités : c'est elles qui rendent « voiture → modèle → marque » naturel.
- Une **migration** est nécessaire à chaque évolution du schéma : prends l'habitude d'en générer une dès que tu touches une entité.
- Prévois quelques **données de départ** (seed) pour ne pas tester sur une base vide.
- Quand un formulaire ne colle pas exactement à une entité, passe par un **ViewModel** dédié plutôt que d'exposer l'entité brute.
- Pour les réservations, **une règle métier bien faite vaut mieux que cinq à moitié** : choisis-en une et traite-la proprement (validation, message clair).

## Pour aller plus loin

- Calculer la **disponibilité** d'une voiture à partir des réservations existantes.
- Ajouter une **recherche / un filtre** par marque ou par modèle.
- Calculer automatiquement le **montant** d'une réservation (tarif journalier × nombre de jours).
- Un petit **tableau de bord** : nombre de voitures, réservations en cours, etc.
- Une présentation **soignée** avec Tailwind.

## Ce qui est attendu

- Le **modèle relationnel est cohérent** (Marque → Modèle → Voiture, et Client / Voiture → Réservation) et correctement mappé en base.
- La base **SQLite est réellement utilisée via EF Core** et persiste les données.
- Le **CRUD voitures est complet et fonctionnel** ; la gestion des clients et des réservations marche.
- Le Projet est "SOLID" complient
- Au moins **une règle métier** est appliquée sur les réservations.
- **Architecture propre** : injection de dépendances, séparation des responsabilités, controllers fins.
- **Validation** des formulaires côté serveur.
- **Code lisible** : encapsulation respectée, nommage clair, pas de catch silencieux ni d'exception générique.