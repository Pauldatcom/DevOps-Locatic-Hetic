namespace Locatic.ViewModels;
using Microsoft.AspNetCore.Http;
public class DashboardViewModel
{
    public int BrandCount { get; set; }
    public int ModeleCount { get; set; }
    public int CarCount { get; set; }
    public int ClientCount { get; set; }
    public int ReservationCount { get; set; }
    public IFormFile? ImageFile { get; set; }
}
