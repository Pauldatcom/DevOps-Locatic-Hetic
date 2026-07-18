using Locatic.Entities;
using Locatic.Services.Implementations;
using Xunit;

namespace Locatic.Tests;

public class CarServiceTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsCarWithModeleAndBrand()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, _, _) = await TestDbHelper.SeedFleetAsync(context);
        // SeedFleet already created one car; create another via service
        var service = new CarService(context);

        await service.CreateAsync(new Car
        {
            RegistrationNumber = "EF-456-GH",
            Year = 2023,
            NumberOfPlaces = 4,
            PricePerDay = 55m,
            FuelType = "Diesel",
            ModeleId = modele.Id
        });

        var cars = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, cars.Count);
        Assert.Contains(cars, c => c.RegistrationNumber == "EF-456-GH");
        Assert.All(cars, c => Assert.NotNull(c.Modele));
        Assert.All(cars, c => Assert.NotNull(c.Modele.Brand));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCar()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new CarService(context);

        var found = await service.GetByIdAsync(car.Id);

        Assert.NotNull(found);
        Assert.Equal("AB-123-CD", found.RegistrationNumber);
        Assert.Equal("208", found.Modele.Name);
        Assert.Equal("Peugeot", found.Modele.Brand.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new CarService(context);

        var found = await service.GetByIdAsync(999);

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersistedFields()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new CarService(context);

        car.PricePerDay = 60m;
        car.FuelType = "Hybride";
        await service.UpdateAsync(car);

        var updated = await service.GetByIdAsync(car.Id);

        Assert.NotNull(updated);
        Assert.Equal(60m, updated.PricePerDay);
        Assert.Equal("Hybride", updated.FuelType);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCar()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new CarService(context);

        await service.DeleteAsync(car.Id);

        Assert.Null(await service.GetByIdAsync(car.Id));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNothing()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new CarService(context);

        await service.DeleteAsync(999);

        Assert.NotNull(await service.GetByIdAsync(car.Id));
    }
}
