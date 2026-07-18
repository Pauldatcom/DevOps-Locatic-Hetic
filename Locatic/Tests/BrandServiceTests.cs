using Locatic.Data;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locatic.Tests;

public class BrandServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsTheCreatedBrand()
    {
        using var context = CreateContext();
        var service = new BrandService(context);

        await service.CreateAsync(new Brand { Name = "Renault", Country = "France" });
        var brands = await service.GetAllAsync();

        Assert.Single(brands);
        Assert.Equal("Renault", brands.First().Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = CreateContext();
        var service = new BrandService(context);

        var brand = await service.GetByIdAsync(999);

        Assert.Null(brand);
    }
}
