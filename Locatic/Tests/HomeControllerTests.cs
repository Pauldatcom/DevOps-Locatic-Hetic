using Locatic.Controllers;
using Locatic.Services.Implementations;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using System.Linq;

namespace Locatic.Tests;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_WithoutFilters_ReturnsAllCars()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new HomeController(new CarService(context), new ModeleService(context));

        var result = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LandingViewModel>(view.Model);
        Assert.Single(model.Cars);
        Assert.Single(model.Modeles);
        Assert.Contains(5, model.AvailablePlaces);
    }

    [Fact]
    public async Task Index_WithModeleFilter_ReturnsMatchingCarsOnly()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, _, _) = await TestDbHelper.SeedFleetAsync(context);
        var otherBrand = new Entities.Brand { Name = "Toyota", Country = "Japon" };
        context.Brands.Add(otherBrand);
        await context.SaveChangesAsync();
        var otherModele = new Entities.Modele { Name = "Yaris", BrandId = otherBrand.Id };
        context.Modeles.Add(otherModele);
        await context.SaveChangesAsync();
        context.Cars.Add(new Entities.Car
        {
            RegistrationNumber = "TY-111-TY",
            Year = 2020,
            NumberOfPlaces = 5,
            PricePerDay = 40m,
            FuelType = "Essence",
            ModeleId = otherModele.Id
        });
        await context.SaveChangesAsync();

        var controller = new HomeController(new CarService(context), new ModeleService(context));

        var result = await controller.Index(modele.Id, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LandingViewModel>(view.Model);
        Assert.Single(model.Cars);
        Assert.Equal(modele.Id, model.Cars.ElementAt(0).ModeleId);
        Assert.Equal(modele.Id, model.SelectedModeleId);
    }

    [Fact]
    public async Task Index_WithPlacesFilter_ReturnsMatchingCarsOnly()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, _, _) = await TestDbHelper.SeedFleetAsync(context);
        context.Cars.Add(new Entities.Car
        {
            RegistrationNumber = "XX-222-XX",
            Year = 2021,
            NumberOfPlaces = 2,
            PricePerDay = 30m,
            FuelType = "Essence",
            ModeleId = modele.Id
        });
        await context.SaveChangesAsync();

        var controller = new HomeController(new CarService(context), new ModeleService(context));

        var result = await controller.Index(null, 2);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LandingViewModel>(view.Model);
        Assert.Single(model.Cars);
        Assert.Equal(2, model.Cars.ElementAt(0).NumberOfPlaces);
        Assert.Equal(2, model.SelectedPlaces);
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new HomeController(new CarService(context), new ModeleService(context));

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }
}
