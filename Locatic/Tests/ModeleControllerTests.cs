using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class ModeleControllerTests
{
    [Fact]
    public async Task Index_ReturnsModeles()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new ModeleController(new ModeleService(context), new BrandService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Modele>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_Get_LoadsBrandsInViewBag()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new ModeleController(new ModeleService(context), new BrandService(context));

        var result = await controller.Create();

        Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.Brands);
    }

    [Fact]
    public async Task Create_Post_ValidModele_RedirectsToIndex()
    {
        using var context = TestDbHelper.CreateContext();
        var (brand, _, _, _) = await TestDbHelper.SeedFleetAsync(context);
        var modeleService = new ModeleService(context);
        var controller = new ModeleController(modeleService, new BrandService(context));

        var result = await controller.Create(new Modele { Name = "3008", BrandId = brand.Id });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ModeleController.Index), redirect.ActionName);
        Assert.Equal(2, (await modeleService.GetAllAsync()).Count());
    }
}
