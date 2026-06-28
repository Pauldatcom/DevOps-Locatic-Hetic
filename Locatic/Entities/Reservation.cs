using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int CarId { get; set; }

    public Car Car { get; set; } = null!;

    public decimal TotalPrice
    {
        get
        {
            var days = (EndDate - StartDate).Days;

            if (days <= 0)
            {
                days = 1;
            }

            return days * Car.PricePerDay;
        }
    }
}