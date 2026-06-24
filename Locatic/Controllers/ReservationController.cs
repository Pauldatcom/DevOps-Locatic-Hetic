using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Locatic.Controllers;

public class ReservationController : Controller
{
    private readonly IReservationService _reservationService;
    private readonly IClientService _clientService;
    private readonly ICarService _carService;

    public ReservationController(
        IReservationService reservationService,
        IClientService clientService,
        ICarService carService)
    {
        _reservationService = reservationService;
        _clientService = clientService;
        _carService = carService;
    }

    public async Task<IActionResult> Index()
    {
        var reservations = await _reservationService.GetAllAsync();
        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadFormLists();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Reservation reservation)
    {
        if (reservation.EndDate < reservation.StartDate)
        {
            ModelState.AddModelError("", "La date de fin doit être après la date de début.");
        }

        if (!ModelState.IsValid)
        {
            await LoadFormLists();
            return View(reservation);
        }

        await _reservationService.CreateAsync(reservation);

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormLists()
    {
        var clients = await _clientService.GetAllAsync();
        var cars = await _carService.GetAllAsync();

        ViewBag.Clients = new SelectList(clients, "Id", "LastName");
        ViewBag.Cars = new SelectList(cars, "Id", "RegistrationNumber");
    }
}