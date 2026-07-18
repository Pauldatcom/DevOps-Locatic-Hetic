using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class BrandControllerTests
{
    [Fact]
    public async Task Index_ReturnsBrands()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new BrandController(new BrandService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Brand>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new BrandController(new BrandService(context));

        var result = controller.Create();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_ValidBrand_RedirectsToIndex()
    {
        using var context = TestDbHelper.CreateContext();
        var brandService = new BrandService(context);
        var controller = new BrandController(brandService);

        var result = await controller.Create(new Brand { Name = "Citroen", Country = "France" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BrandController.Index), redirect.ActionName);
        Assert.Single(await brandService.GetAllAsync());
    }
}
