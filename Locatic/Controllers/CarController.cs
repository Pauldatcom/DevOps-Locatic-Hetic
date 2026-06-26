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
        await LoadModeles();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Car car)
    {
        if (!ModelState.IsValid)
        {
            await LoadModeles();
            return View(car);
        }

        await _carService.CreateAsync(car);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car == null)
        {
            return NotFound();
        }

        await LoadModeles();
        return View(car);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Car car)
    {
        if (!ModelState.IsValid)
        {
            await LoadModeles();
            return View(car);
        }

        await _carService.UpdateAsync(car);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _carService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadModeles()
    {
        var modeles = await _modeleService.GetAllAsync();
        ViewBag.Modeles = new SelectList(modeles, "Id", "Name");
    }
}