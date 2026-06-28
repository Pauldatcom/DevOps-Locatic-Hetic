using System.Diagnostics;
using Locatic.Models;
using Locatic.Services.Interfaces;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Controllers;

public class HomeController : Controller
{
    private readonly ICarService _carService;
    private readonly IModeleService _modeleService;

    public HomeController(ICarService carService, IModeleService modeleService)
    {
        _carService = carService;
        _modeleService = modeleService;
    }

    public async Task<IActionResult> Index(int? modeleId, int? places)
    {
        var allCars = (await _carService.GetAllAsync()).ToList();
        var modeles = (await _modeleService.GetAllAsync()).ToList();
        var availablePlaces = allCars.Select(c => c.NumberOfPlaces).Distinct().OrderBy(p => p).ToList();

        var cars = allCars.AsEnumerable();

        if (modeleId.HasValue)
        {
            cars = cars.Where(c => c.ModeleId == modeleId.Value);
        }

        if (places.HasValue)
        {
            cars = cars.Where(c => c.NumberOfPlaces == places.Value);
        }

        var viewModel = new LandingViewModel
        {
            Cars = cars.ToList(),
            Modeles = modeles,
            AvailablePlaces = availablePlaces,
            SelectedModeleId = modeleId,
            SelectedPlaces = places
        };

        ViewData["FullWidth"] = true;
        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
