using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class CarControllerTests
{
    [Fact]
    public async Task Index_ReturnsCarViewModels()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new CarController(new CarService(context), new ModeleService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<CarViewModel>>(view.Model);
        Assert.Single(model);
        Assert.Equal("AB-123-CD", model.First().RegistrationNumber);
    }

    [Fact]
    public async Task Details_UnknownId_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new CarController(new CarService(context), new ModeleService(context));

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewModel()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var controller = new CarController(new CarService(context), new ModeleService(context));

        var result = await controller.Details(car.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CarViewModel>(view.Model);
        Assert.Equal(car.Id, model.Id);
        Assert.Equal("AB-123-CD", model.RegistrationNumber);
    }

    [Fact]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new CarController(new CarService(context), new ModeleService(context));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new CarController(new CarService(context), new ModeleService(context));

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesCarAndRedirects()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, car, _) = await TestDbHelper.SeedFleetAsync(context);
        var carService = new CarService(context);
        var controller = new CarController(carService, new ModeleService(context));

        var result = await controller.DeleteConfirmed(car.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CarController.Index), redirect.ActionName);
        Assert.Null(await carService.GetByIdAsync(car.Id));
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, _, _) = await TestDbHelper.SeedFleetAsync(context);
        var carService = new CarService(context);
        var controller = new CarController(carService, new ModeleService(context));

        var result = await controller.Create(new CarViewModel
        {
            RegistrationNumber = "NEW-001",
            Year = 2024,
            NumberOfPlaces = 5,
            PricePerDay = 70m,
            FuelType = "Electrique",
            ModeleId = modele.Id
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CarController.Index), redirect.ActionName);
        Assert.Contains(await carService.GetAllAsync(), c => c.RegistrationNumber == "NEW-001");
    }
}
