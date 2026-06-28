using Locatic.Services.Interfaces;
using Locatic.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locatic.Controllers;

public class AdminController : Controller
{
    private readonly IBrandService _brandService;
    private readonly IModeleService _modeleService;
    private readonly ICarService _carService;
    private readonly IClientService _clientService;
    private readonly IReservationService _reservationService;

    public AdminController(
        IBrandService brandService,
        IModeleService modeleService,
        ICarService carService,
        IClientService clientService,
        IReservationService reservationService)
    {
        _brandService = brandService;
        _modeleService = modeleService;
        _carService = carService;
        _clientService = clientService;
        _reservationService = reservationService;
    }

    public async Task<IActionResult> Index()
    {
        var brands = await _brandService.GetAllAsync();
        var modeles = await _modeleService.GetAllAsync();
        var cars = await _carService.GetAllAsync();
        var clients = await _clientService.GetAllAsync();
        var reservations = await _reservationService.GetAllAsync();

        var dashboard = new DashboardViewModel
        {
            BrandCount = brands.Count(),
            ModeleCount = modeles.Count(),
            CarCount = cars.Count(),
            ClientCount = clients.Count(),
            ReservationCount = reservations.Count()
        };

        return View(dashboard);
    }
}
