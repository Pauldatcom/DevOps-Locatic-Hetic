using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities;

public class Car
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Range(1990, 2030)]
    public int Year { get; set; }

    [Range(1, 9)]
    public int NumberOfPlaces { get; set; }

    [Range(1, 1000)]
    public decimal PricePerDay { get; set; }

    [Required]
    [StringLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ModeleId { get; set; }

    public Modele Modele { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}