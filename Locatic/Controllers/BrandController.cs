using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Controllers;

public class BrandController : Controller
{
    private readonly IBrandService _brandService;

    public BrandController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    public async Task<IActionResult> Index()
    {
        var brands = await _brandService.GetAllAsync();
        return View(brands);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Brand brand)
    {
        if (!ModelState.IsValid)
        {
            return View(brand);
        }

        await _brandService.CreateAsync(brand);

        return RedirectToAction(nameof(Index));
    }
}