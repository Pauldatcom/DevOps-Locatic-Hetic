using System.ComponentModel.DataAnnotations;

namespace Locatic.ViewModels;

public class ReservationBookViewModel
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CarId { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date de début")]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "La date de fin est obligatoire.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date de fin")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(3);

    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    [StringLength(100)]
    [Display(Name = "Prénom")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(100)]
    [Display(Name = "Nom")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'e-mail est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Téléphone")]
    public string? PhoneNumber { get; set; }

    public string CarName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal PricePerDay { get; set; }
    public int Year { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public int NumberOfPlaces { get; set; }
}
