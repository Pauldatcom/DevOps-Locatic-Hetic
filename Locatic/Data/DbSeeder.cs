using Locatic.Entities;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        context.Database.Migrate();

        SeedBrands(context);
        SeedModeles(context);
        SeedCars(context);
        SeedClients(context);
        UpdateCarImages(context);
    }

    private static void SeedBrands(AppDbContext context)
    {
        if (context.Brands.Any())
        {
            return;
        }

        context.Brands.AddRange(
            new Brand { Name = "Renault", Country = "France" },
            new Brand { Name = "Peugeot", Country = "France" },
            new Brand { Name = "BMW", Country = "Allemagne" }
        );

        context.SaveChanges();
    }

    private static void SeedModeles(AppDbContext context)
    {
        if (context.Modeles.Any())
        {
            return;
        }

        var renault = context.Brands.FirstOrDefault(b => b.Name == "Renault") ?? context.Brands.First();
        var peugeot = context.Brands.FirstOrDefault(b => b.Name == "Peugeot") ?? context.Brands.First();
        var bmw = context.Brands.FirstOrDefault(b => b.Name == "BMW") ?? context.Brands.First();

        context.Modeles.AddRange(
            new Modele { Name = "Clio", BrandId = renault.Id },
            new Modele { Name = "208", BrandId = peugeot.Id },
            new Modele { Name = "Série 3", BrandId = bmw.Id }
        );

        context.SaveChanges();
    }

    private static void SeedCars(AppDbContext context)
    {
        if (context.Cars.Any())
        {
            return;
        }

        var clio = context.Modeles.FirstOrDefault(m => m.Name == "Clio") ?? context.Modeles.First();
        var peugeot208 = context.Modeles.FirstOrDefault(m => m.Name == "208") ?? context.Modeles.First();
        var serie3 = context.Modeles.FirstOrDefault(m => m.Name == "Série 3") ?? context.Modeles.First();

        context.Cars.AddRange(
            new Car
            {
                RegistrationNumber = "AA-123-BB",
                Year = 2022,
                NumberOfPlaces = 5,
                PricePerDay = 45,
                FuelType = "Essence",
                ModeleId = clio.Id,
                ImageUrl = "https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=600&h=400&fit=crop"
            },
            new Car
            {
                RegistrationNumber = "CC-456-DD",
                Year = 2023,
                NumberOfPlaces = 5,
                PricePerDay = 50,
                FuelType = "Diesel",
                ModeleId = peugeot208.Id,
                ImageUrl = "https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=600&h=400&fit=crop"
            },
            new Car
            {
                RegistrationNumber = "EE-789-FF",
                Year = 2021,
                NumberOfPlaces = 5,
                PricePerDay = 90,
                FuelType = "Essence",
                ModeleId = serie3.Id,
                ImageUrl = "https://images.unsplash.com/photo-1555215695-3004980ad54e?w=600&h=400&fit=crop"
            }
        );

        context.SaveChanges();
    }

    private static void UpdateCarImages(AppDbContext context)
    {
        var imageByRegistration = new Dictionary<string, string>
        {
            ["AA-123-BB"] = "https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=600&h=400&fit=crop",
            ["CC-456-DD"] = "https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=600&h=400&fit=crop",
            ["EE-789-FF"] = "https://images.unsplash.com/photo-1555215695-3004980ad54e?w=600&h=400&fit=crop"
        };

        foreach (var car in context.Cars.Where(c => string.IsNullOrWhiteSpace(c.ImageUrl)))
        {
            if (imageByRegistration.TryGetValue(car.RegistrationNumber, out var imageUrl))
            {
                car.ImageUrl = imageUrl;
            }
        }

        context.SaveChanges();
    }

    private static void SeedClients(AppDbContext context)
    {
        if (context.Clients.Any())
        {
            return;
        }

        context.Clients.AddRange(
            new Client
            {
                LastName = "Dupont",
                FirstName = "Jean",
                Email = "jean.dupont@example.com",
                PhoneNumber = "0600000001"
            },
            new Client
            {
                LastName = "Martin",
                FirstName = "Claire",
                Email = "claire.martin@example.com",
                PhoneNumber = "0600000002"
            }
        );

        context.SaveChanges();
    }
}