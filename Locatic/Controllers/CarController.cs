using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Locatic.Controllers;

public class CarController : Controller
{
    private readonly ICarService _carService;
    private readonly IModeleService _modeleService;

    public CarController(ICarService carService, IModeleService modeleService)
    {
        _carService = carService;
        _modeleService = modeleService;
    }

    public async Task<IActionResult> Index()
    {
        var cars = await _carService.GetAllAsync();
        return View(cars);
    }

    public async Task<IActionResult> Details(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var modeles = await _modeleService.GetAllAsync();
        ViewBag.Modeles = new SelectList(modeles, "Id", "Name");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Car car)
    {
        if (!ModelState.IsValid)
        {
            var modeles = await _modeleService.GetAllAsync();
            ViewBag.Modeles = new SelectList(modeles, "Id", "Name");

            return View(car);
        }

        await _carService.CreateAsync(car);

        return RedirectToAction(nameof(Index));
    }
}