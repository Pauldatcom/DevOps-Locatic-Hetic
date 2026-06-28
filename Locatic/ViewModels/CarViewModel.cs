using Locatic.Entities;
using Microsoft.AspNetCore.Http;

namespace Locatic.ViewModels;

public class CarViewModel
{
    public int Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public int Year { get; set; }

    public int NumberOfPlaces { get; set; }

    public decimal PricePerDay { get; set; }

    public string FuelType { get; set; } = string.Empty;

    public int ModeleId { get; set; }

    public Modele? Modele { get; set; }

    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }
}