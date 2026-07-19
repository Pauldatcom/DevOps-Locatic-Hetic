using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class ReservationControllerTests
{
    [Fact]
    public async Task Index_ReturnsReservations()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var reservationService = new ReservationService(context);
        await reservationService.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 5)
        });

        var controller = new ReservationController(
            reservationService,
            new ClientService(context),
            new CarService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Reservation>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Book_Get_UnknownCar_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new ReservationController(
            new ReservationService(context),
            new ClientService(context),
            new CarService(context));

        var result = await controller.Book(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Book_Get_ExistingCar_ReturnsBookViewModel()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var controller = new ReservationController(
            new ReservationService(context),
            new ClientService(context),
            new CarService(context));

        var result = await controller.Book(car.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReservationBookViewModel>(view.Model);
        Assert.Equal(car.Id, model.CarId);
        Assert.Equal("208", model.CarName);
        Assert.Equal("Peugeot", model.BrandName);
        Assert.Equal(45m, model.PricePerDay);
    }

    [Fact]
    public async Task Book_Post_EndBeforeStart_ReturnsViewWithError()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var controller = new ReservationController(
            new ReservationService(context),
            new ClientService(context),
            new CarService(context));

        var result = await controller.Book(new ReservationBookViewModel
        {
            CarId = car.Id,
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean@example.com",
            StartDate = new DateTime(2026, 8, 10),
            EndDate = new DateTime(2026, 8, 5)
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.IsType<ReservationBookViewModel>(view.Model);
    }

    [Fact]
    public async Task Book_Post_ValidRequest_ReturnsConfirmation()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var reservationService = new ReservationService(context);
        var clientService = new ClientService(context);
        var controller = new ReservationController(
            reservationService,
            clientService,
            new CarService(context));

        var start = DateTime.Today.AddDays(7);
        var end = start.AddDays(3);

        var result = await controller.Book(new ReservationBookViewModel
        {
            CarId = car.Id,
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean@example.com",
            PhoneNumber = "0622222222",
            StartDate = start,
            EndDate = end
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Confirmation", view.ViewName);
        var confirmation = Assert.IsType<ReservationConfirmationViewModel>(view.Model);
        Assert.Equal("Jean Dupont", confirmation.ClientName);
        Assert.Equal(3, confirmation.Days);
        Assert.Equal(135m, confirmation.TotalPrice);
        Assert.Single(await reservationService.GetAllAsync());
        Assert.Equal(2, (await clientService.GetAllAsync()).Count());
    }

    [Fact]
    public async Task Create_Post_UnavailableCar_ReturnsViewWithError()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        var reservationService = new ReservationService(context);
        await reservationService.CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 10)
        });

        var controller = new ReservationController(
            reservationService,
            new ClientService(context),
            new CarService(context));

        var result = await controller.Create(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 5),
            EndDate = new DateTime(2026, 8, 12)
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }
}
