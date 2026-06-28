namespace Locatic.ViewModels;

public class ReservationConfirmationViewModel
{
    public string CarName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public int Days { get; set; }
}
