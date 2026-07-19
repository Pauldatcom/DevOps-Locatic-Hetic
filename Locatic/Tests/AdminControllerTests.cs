using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task Index_ReturnsDashboardCounts()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, client) = await TestDbHelper.SeedFleetAsync(context);
        await new ReservationService(context).CreateAsync(new Reservation
        {
            CarId = car.Id,
            ClientId = client.Id,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 3)
        });

        var controller = new AdminController(
            new BrandService(context),
            new ModeleService(context),
            new CarService(context),
            new ClientService(context),
            new ReservationService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(1, model.BrandCount);
        Assert.Equal(1, model.ModeleCount);
        Assert.Equal(1, model.CarCount);
        Assert.Equal(1, model.ClientCount);
        Assert.Equal(1, model.ReservationCount);
    }

    [Fact]
    public async Task Index_EmptyDatabase_ReturnsZeroCounts()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new AdminController(
            new BrandService(context),
            new ModeleService(context),
            new CarService(context),
            new ClientService(context),
            new ReservationService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(0, model.BrandCount);
        Assert.Equal(0, model.ModeleCount);
        Assert.Equal(0, model.CarCount);
        Assert.Equal(0, model.ClientCount);
        Assert.Equal(0, model.ReservationCount);
    }
}
