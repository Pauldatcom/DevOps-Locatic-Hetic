using Locatic.Entities;
using Locatic.Services.Interfaces;
using Locatic.ViewModels;
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
    public async Task<IActionResult> Book(int carId)
    {
        var car = await _carService.GetByIdAsync(carId);

        if (car == null)
        {
            return NotFound();
        }

        var viewModel = MapCarToBookViewModel(car);
        ViewData["FullWidth"] = true;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(ReservationBookViewModel model)
    {
        var car = await _carService.GetByIdAsync(model.CarId);

        if (car == null)
        {
            return NotFound();
        }

        if (model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "La date de fin doit être après la date de début.");
        }

        if (model.StartDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.StartDate), "La date de début ne peut pas être dans le passé.");
        }

        if (ModelState.IsValid)
        {
            var isAvailable = await _reservationService.IsCarAvailableAsync(
                model.CarId,
                model.StartDate,
                model.EndDate
            );

            if (!isAvailable)
            {
                ModelState.AddModelError("", "Cette voiture est déjà réservée sur cette période. Choisissez d'autres dates.");
            }
        }

        if (!ModelState.IsValid)
        {
            PopulateCarDetails(model, car);
            ViewData["FullWidth"] = true;
            return View(model);
        }

        var client = new Client
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber
        };

        await _clientService.CreateAsync(client);

        var reservation = new Reservation
        {
            CarId = model.CarId,
            ClientId = client.Id,
            StartDate = model.StartDate,
            EndDate = model.EndDate
        };

        await _reservationService.CreateAsync(reservation);

        var days = (model.EndDate - model.StartDate).Days;
        if (days <= 0)
        {
            days = 1;
        }

        var confirmation = new ReservationConfirmationViewModel
        {
            CarName = car.Modele.Name,
            BrandName = car.Modele.Brand.Name,
            ImageUrl = car.ImageUrl,
            ClientName = $"{client.FirstName} {client.LastName}",
            Email = client.Email,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Days = days,
            TotalPrice = days * car.PricePerDay
        };

        ViewData["FullWidth"] = true;
        return View("Confirmation", confirmation);
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

        var isAvailable = await _reservationService.IsCarAvailableAsync(
            reservation.CarId,
            reservation.StartDate,
            reservation.EndDate
        );

        if (!isAvailable)
        {
            ModelState.AddModelError("", "Cette voiture est déjà réservée sur cette période.");
        }

        if (!ModelState.IsValid)
        {
            await LoadFormLists();
            return View(reservation);
        }

        await _reservationService.CreateAsync(reservation);
        return RedirectToAction(nameof(Index));
    }

    private static ReservationBookViewModel MapCarToBookViewModel(Car car)
    {
        return new ReservationBookViewModel
        {
            CarId = car.Id,
            CarName = car.Modele.Name,
            BrandName = car.Modele.Brand.Name,
            ImageUrl = car.ImageUrl,
            PricePerDay = car.PricePerDay,
            Year = car.Year,
            FuelType = car.FuelType,
            NumberOfPlaces = car.NumberOfPlaces
        };
    }

    private static void PopulateCarDetails(ReservationBookViewModel model, Car car)
    {
        model.CarName = car.Modele.Name;
        model.BrandName = car.Modele.Brand.Name;
        model.ImageUrl = car.ImageUrl;
        model.PricePerDay = car.PricePerDay;
        model.Year = car.Year;
        model.FuelType = car.FuelType;
        model.NumberOfPlaces = car.NumberOfPlaces;
    }

    private async Task LoadFormLists()
    {
        var clients = await _clientService.GetAllAsync();
        var cars = await _carService.GetAllAsync();

        ViewBag.Clients = new SelectList(clients, "Id", "LastName");
        ViewBag.Cars = new SelectList(cars, "Id", "RegistrationNumber");
    }
}
