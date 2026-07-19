using Locatic.Data;
using Locatic.Entities;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Tests;

internal static class TestDbHelper
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static async Task<(Brand brand, Modele modele, Car car, Client client)> SeedFleetAsync(AppDbContext context)
    {
        var brand = new Brand { Name = "Peugeot", Country = "France" };
        context.Brands.Add(brand);
        await context.SaveChangesAsync();

        var modele = new Modele { Name = "208", BrandId = brand.Id };
        context.Modeles.Add(modele);
        await context.SaveChangesAsync();

        var car = new Car
        {
            RegistrationNumber = "AB-123-CD",
            Year = 2022,
            NumberOfPlaces = 5,
            PricePerDay = 45m,
            FuelType = "Essence",
            ModeleId = modele.Id,
            ImageUrl = "/images/208.jpg"
        };
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var client = new Client
        {
            FirstName = "Alice",
            LastName = "Martin",
            Email = "alice@example.com",
            PhoneNumber = "0600000000"
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        return (brand, modele, car, client);
    }
}
