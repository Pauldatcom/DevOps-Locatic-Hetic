using Locatic.Entities;
using Locatic.Services.Implementations;
using Xunit;

namespace Locatic.Tests;

public class BrandServiceTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsTheCreatedBrand()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new BrandService(context);

        await service.CreateAsync(new Brand { Name = "Renault", Country = "France" });
        var brands = await service.GetAllAsync();

        Assert.Single(brands);
        Assert.Equal("Renault", brands.First().Name);
        Assert.Equal("France", brands.First().Country);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBrand()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new BrandService(context);
        await service.CreateAsync(new Brand { Name = "BMW", Country = "Allemagne" });
        var created = (await service.GetAllAsync()).First();

        var brand = await service.GetByIdAsync(created.Id);

        Assert.NotNull(brand);
        Assert.Equal("BMW", brand.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new BrandService(context);

        var brand = await service.GetByIdAsync(999);

        Assert.Null(brand);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new BrandService(context);

        var brands = await service.GetAllAsync();

        Assert.Empty(brands);
    }
}
