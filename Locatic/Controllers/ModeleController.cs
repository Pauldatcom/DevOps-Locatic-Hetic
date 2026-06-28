using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Locatic.Controllers;

public class ModeleController : Controller
{
    private readonly IModeleService _modeleService;
    private readonly IBrandService _brandService;

    public ModeleController(IModeleService modeleService, IBrandService brandService)
    {
        _modeleService = modeleService;
        _brandService = brandService;
    }

    public async Task<IActionResult> Index()
    {
        var modeles = await _modeleService.GetAllAsync();
        return View(modeles);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var brands = await _brandService.GetAllAsync();
        ViewBag.Brands = new SelectList(brands, "Id", "Name");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Modele modele)
    {
        if (!ModelState.IsValid)
        {
            var brands = await _brandService.GetAllAsync();
            ViewBag.Brands = new SelectList(brands, "Id", "Name");

            return View(modele);
        }

        await _modeleService.CreateAsync(modele);

        return RedirectToAction(nameof(Index));
    }
}