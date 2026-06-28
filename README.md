<<<<<<< HEAD
# Locatic

Projet ASP.NET Core MVC de gestion d'une agence de location de voitures.

## Technologies

- ASP.NET Core MVC
- Entity Framework Core
- SQLite
- Razor Views

## Membres

- Esso 
- Gires
=======
# 🚗 Locatic

Locatic est une application web développée en ASP.NET Core MVC permettant la gestion d'une agence de location de voitures.

Le projet permet de gérer :

- Les marques de véhicules
- Les modèles de véhicules
- Le parc automobile
- Les clients
- Les réservations

Les données sont persistées dans une base SQLite via Entity Framework Core.

---

#  Membres du binôme

- Esso Mawaki ASSIAH
- Gires TIENTCHEU

---

#  Technologies utilisées

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core
- SQLite
- Razor Views
- Bootstrap 5
- Git / GitHub

---

#  Architecture du projet

```txt
Locatic
│
├── Controllers
├── Data
├── Entities
├── Migrations
├── Services
│   ├── Interfaces
│   └── Implementations
├── ViewModels
├── Views
├── wwwroot
│
└── Program.cs
```

Le projet suit une architecture en couches :

- Entités métier
- Accès aux données (EF Core)
- Services métier
- Contrôleurs MVC
- Vues Razor

Les contrôleurs restent légers et délèguent la logique métier aux services.

---

#  Modèle de données

```txt
Brand
  │
  └───< Modele
            │
            └───< Car
                       │
                       └───< Reservation >─── Client
```

Relations :

- Une marque possède plusieurs modèles.
- Un modèle appartient à une seule marque.
- Un modèle possède plusieurs voitures.
- Une voiture appartient à un seul modèle.
- Un client possède plusieurs réservations.
- Une réservation concerne une seule voiture.
- Une réservation concerne un seul client.

---

#  Fonctionnalités réalisées

## Marques

- Création d'une marque
- Liste des marques

## Modèles

- Création d'un modèle
- Association à une marque existante
- Liste des modèles

## Voitures

- Création d'une voiture
- Association à un modèle existant
- Liste des voitures
- Consultation du détail d'une voiture

## Clients

- Création d'un client
- Liste des clients

## Réservations

- Création d'une réservation
- Liste des réservations

---

#  Fonctionnalités en cours

- Validation avancée des formulaires
- CRUD complet des voitures
- Gestion de la disponibilité des véhicules
- Amélioration de la règle métier des réservations
- Tableau de bord
- Seed de données

---

#  Installation

## 1. Cloner le projet

```bash
git clone https://github.com/Louange-03/Projet_Locatic.git
```

```bash
cd Projet_Locatic
```

---

## 2. Restaurer les dépendances

```bash
dotnet restore
```

---

## 3. Appliquer les migrations

```bash
cd Locatic
```

```bash
dotnet ef database update
```

---

## 4. Lancer l'application

```bash
dotnet run
```

L'application est ensuite disponible à l'adresse :

```txt
http://localhost:5286
```

---

#  Commandes utiles

Créer une migration :

```bash
dotnet ef migrations add NomMigration
```

Mettre à jour la base :

```bash
dotnet ef database update
```

Compiler le projet :

```bash
dotnet build
```

---

#  Convention Git

## Branches principales

```txt
main
develop
```

## Branches de travail

```txt
feature/project-setup
feature/entities
feature/database-cleanup
```

Chaque fonctionnalité est développée sur une branche dédiée puis fusionnée dans `develop`.

Une fois validée, elle est intégrée dans `main`.

---

#  État actuel du projet

```txt
✔ Architecture MVC
✔ SQLite
✔ Entity Framework Core
✔ Services métier
✔ Injection de dépendances
✔ Gestion des marques
✔ Gestion des modèles
✔ Gestion des voitures
✔ Gestion des clients
✔ Gestion des réservations

Prochaine étape :
→ Validation métier
→ CRUD complet
→ Disponibilité des véhicules
→ Dashboard
```
>>>>>>> review-gires-latest
