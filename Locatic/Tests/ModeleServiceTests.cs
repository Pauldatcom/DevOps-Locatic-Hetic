using Locatic.Entities;
using Locatic.Services.Implementations;
using Xunit;

namespace Locatic.Tests;

public class ModeleServiceTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsModeleWithBrand()
    {
        using var context = TestDbHelper.CreateContext();
        var brand = new Brand { Name = "Renault", Country = "France" };
        context.Brands.Add(brand);
        await context.SaveChangesAsync();

        var service = new ModeleService(context);
        await service.CreateAsync(new Modele { Name = "Clio", BrandId = brand.Id });

        var modeles = (await service.GetAllAsync()).ToList();

        Assert.Single(modeles);
        Assert.Equal("Clio", modeles[0].Name);
        Assert.NotNull(modeles[0].Brand);
        Assert.Equal("Renault", modeles[0].Brand.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsModele()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, modele, _, _) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ModeleService(context);

        var found = await service.GetByIdAsync(modele.Id);

        Assert.NotNull(found);
        Assert.Equal("208", found.Name);
        Assert.Equal("Peugeot", found.Brand.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new ModeleService(context);

        Assert.Null(await service.GetByIdAsync(999));
    }
}
