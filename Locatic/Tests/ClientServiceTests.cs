using Locatic.Entities;
using Locatic.Services.Implementations;
using Xunit;

namespace Locatic.Tests;

public class ClientServiceTests
{
    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsTheCreatedClient()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new ClientService(context);

        await service.CreateAsync(new Client
        {
            FirstName = "Bob",
            LastName = "Dupont",
            Email = "bob@example.com",
            PhoneNumber = "0611111111"
        });

        var clients = await service.GetAllAsync();

        Assert.Single(clients);
        Assert.Equal("Bob", clients.First().FirstName);
        Assert.Equal("Dupont", clients.First().LastName);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = TestDbHelper.CreateContext();
        var service = new ClientService(context);

        Assert.Null(await service.GetByIdAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_ChangesEmail()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, _, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ClientService(context);

        client.Email = "alice.updated@example.com";
        await service.UpdateAsync(client);

        var updated = await service.GetByIdAsync(client.Id);

        Assert.NotNull(updated);
        Assert.Equal("alice.updated@example.com", updated.Email);
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, _, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ClientService(context);

        await service.DeleteAsync(client.Id);

        Assert.Null(await service.GetByIdAsync(client.Id));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNothing()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, _, client) = await TestDbHelper.SeedFleetAsync(context);
        var service = new ClientService(context);

        await service.DeleteAsync(999);

        Assert.NotNull(await service.GetByIdAsync(client.Id));
    }
}
