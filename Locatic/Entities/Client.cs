using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities;

public class Client
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}