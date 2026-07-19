using Locatic.Controllers;
using Locatic.Entities;
using Locatic.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Locatic.Tests;

public class ClientControllerTests
{
    [Fact]
    public async Task Index_ReturnsClients()
    {
        using var context = TestDbHelper.CreateContext();
        await TestDbHelper.SeedFleetAsync(context);
        var controller = new ClientController(new ClientService(context));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Client>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Edit_Get_UnknownId_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new ClientController(new ClientService(context));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsClient()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, _, client) = await TestDbHelper.SeedFleetAsync(context);
        var controller = new ClientController(new ClientService(context));

        var result = await controller.Edit(client.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Client>(view.Model);
        Assert.Equal("Alice", model.FirstName);
    }

    [Fact]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        using var context = TestDbHelper.CreateContext();
        var controller = new ClientController(new ClientService(context));

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesClientAndRedirects()
    {
        using var context = TestDbHelper.CreateContext();
        var (_, _, _, client) = await TestDbHelper.SeedFleetAsync(context);
        var clientService = new ClientService(context);
        var controller = new ClientController(clientService);

        var result = await controller.DeleteConfirmed(client.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ClientController.Index), redirect.ActionName);
        Assert.Null(await clientService.GetByIdAsync(client.Id));
    }

    [Fact]
    public async Task Create_Post_ValidClient_RedirectsToIndex()
    {
        using var context = TestDbHelper.CreateContext();
        var clientService = new ClientService(context);
        var controller = new ClientController(clientService);

        var result = await controller.Create(new Client
        {
            FirstName = "Claire",
            LastName = "Bernard",
            Email = "claire@example.com"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ClientController.Index), redirect.ActionName);
        Assert.Single(await clientService.GetAllAsync());
    }
}
