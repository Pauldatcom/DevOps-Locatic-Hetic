using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Controllers;

public class ClientController : Controller
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    public async Task<IActionResult> Index()
    {
        var clients = await _clientService.GetAllAsync();
        return View(clients);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        await _clientService.CreateAsync(client);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Client client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        await _clientService.UpdateAsync(client);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientService.GetByIdAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _clientService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}