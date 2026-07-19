using Locatic.Entities;
using Locatic.Services.Implementations;
using Xunit;

namespace Locatic.Tests;

public class ReservationServiceTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_IncludesClientAndCar()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ReservationService(context);

        await service.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 5)
        });

        var reservations = (await service.GetAllAsync()).ToList();

        Assert.Single(reservations);
        Assert.Equal(client.Id, reservations[0].ClientId);
        Assert.Equal(car.Id, reservations[0].CarId);
        Assert.NotNull(reservations[0].Client);
        Assert.NotNull(reservations[0].Car.Modele.Brand);
    }

    [Fact]
    public async Task IsCarAvailableAsync_NoExistingReservation_ReturnsTrue()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ReservationService(context);

        var available = await service.IsCarAvailableAsync(
            car.Id,
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 5));

        Assert.True(available);
    }

    [Fact]
    public async Task IsCarAvailableAsync_OverlappingPeriod_ReturnsFalse()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ReservationService(context);

        await service.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 10)
        });

        var available = await service.IsCarAvailableAsync(
            car.Id,
            new DateTime(2026, 8, 5),
            new DateTime(2026, 8, 12));

        Assert.False(available);
    }

    [Fact]
    public async Task IsCarAvailableAsync_NonOverlappingPeriod_ReturnsTrue()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ReservationService(context);

        await service.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 5)
        });

        var available = await service.IsCarAvailableAsync(
            car.Id,
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 15));

        Assert.True(available);
    }

    [Fact]
    public async Task IsCarAvailableAsync_DifferentCar_ReturnsTrue()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var otherCar = new Car
        {
            RegistrationNumber = "ZZ-999-ZZ",
            Year = 2021,
            NumberOfPlaces = 5,
            PricePerDay = 40m,
            FuelType = "Essence",
            ModeleId = modele.Id
        };
        context.Cars.Add(otherCar);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        await service.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 10)
        });

        var available = await service.IsCarAvailableAsync(
            otherCar.Id,
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 10));

        Assert.True(available);
    }

    [Fact]
    public void TotalPrice_MultiDayStay_MultipliesDaysByPricePerDay()
    {
        var car = new Car { PricePerDay = 50m };
        var reservation = new Reservation
        {
            Car = car,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 4)
        };

        Assert.Equal(150m, reservation.TotalPrice);
    }

    [Fact]
    public void TotalPrice_SameDay_CountsAsOneDay()
    {
        var car = new Car { PricePerDay = 50m };
        var reservation = new Reservation
        {
            Car = car,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 1)
        };

        Assert.Equal(50m, reservation.TotalPrice);
    }
}
