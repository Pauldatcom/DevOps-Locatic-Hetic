using Locatic.Entities;
using Locatic.Services.Interfaces;
using Locatic.ViewModels;
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

    // ================= INDEX =================
    public async Task<IActionResult> Index()
    {
        var cars = await _carService.GetAllAsync();

        var model = cars.Select(c => new CarViewModel
        {
            Id = c.Id,
            RegistrationNumber = c.RegistrationNumber,
            Year = c.Year,
            NumberOfPlaces = c.NumberOfPlaces,
            PricePerDay = c.PricePerDay,
            FuelType = c.FuelType,
            ModeleId = c.ModeleId,
            Modele = c.Modele,
            ImageUrl = c.ImageUrl
        }).ToList();

        return View(model);
    }

    // ================= DETAILS =================
    public async Task<IActionResult> Details(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car is null)
            return NotFound();

        var model = new CarViewModel
        {
            Id = car.Id,
            RegistrationNumber = car.RegistrationNumber,
            Year = car.Year,
            NumberOfPlaces = car.NumberOfPlaces,
            PricePerDay = car.PricePerDay,
            FuelType = car.FuelType,
            ModeleId = car.ModeleId,
            Modele = car.Modele,
            ImageUrl = car.ImageUrl
        };

        return View(model);
    }

    // ================= CREATE (GET) =================
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadModeles();
        return View();
    }

    // ================= CREATE (POST) =================
    [HttpPost]
    public async Task<IActionResult> Create(CarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadModeles();
            return View(model);
        }

        string imageUrl = "";

        if (model.ImageFile != null)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadDirectory);

            var path = Path.Combine(uploadDirectory, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            imageUrl = "/images/" + fileName;
        }

        var car = new Car
        {
            RegistrationNumber = model.RegistrationNumber,
            Year = model.Year,
            NumberOfPlaces = model.NumberOfPlaces,
            PricePerDay = model.PricePerDay,
            FuelType = model.FuelType,
            ModeleId = model.ModeleId,
            ImageUrl = imageUrl
        };

        await _carService.CreateAsync(car);

        return RedirectToAction(nameof(Index));
    }

    // ================= EDIT (GET) =================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car is null)
            return NotFound();

        await LoadModeles();

        var model = new CarViewModel
        {
            Id = car.Id,
            RegistrationNumber = car.RegistrationNumber,
            Year = car.Year,
            NumberOfPlaces = car.NumberOfPlaces,
            PricePerDay = car.PricePerDay,
            FuelType = car.FuelType,
            ModeleId = car.ModeleId,
            Modele = car.Modele,
            ImageUrl = car.ImageUrl
        };

        return View(model);
    }

    // ================= EDIT (POST) =================
    [HttpPost]
    public async Task<IActionResult> Edit(CarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadModeles();
            return View(model);
        }

        var car = await _carService.GetByIdAsync(model.Id);

        if (car is null)
            return NotFound();

        car.RegistrationNumber = model.RegistrationNumber;
        car.Year = model.Year;
        car.NumberOfPlaces = model.NumberOfPlaces;
        car.PricePerDay = model.PricePerDay;
        car.FuelType = model.FuelType;
        car.ModeleId = model.ModeleId;

        if (model.ImageFile != null)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadDirectory);

            var path = Path.Combine(uploadDirectory, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            car.ImageUrl = "/images/" + fileName;
        }

        await _carService.UpdateAsync(car);

        return RedirectToAction(nameof(Index));
    }

    // ================= DELETE =================
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var car = await _carService.GetByIdAsync(id);

        if (car is null)
            return NotFound();

        return View(car);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _carService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // ================= MODELES =================
    private async Task LoadModeles()
    {
        var modeles = await _modeleService.GetAllAsync();
        ViewBag.Modeles = new SelectList(modeles, "Id", "Name");
    }
}