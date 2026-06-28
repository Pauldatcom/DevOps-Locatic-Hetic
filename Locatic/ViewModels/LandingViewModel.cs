using Locatic.Entities;

namespace Locatic.ViewModels;

public class LandingViewModel
{
    public IEnumerable<Car> Cars { get; set; } = [];
    public IEnumerable<Modele> Modeles { get; set; } = [];
    public IEnumerable<int> AvailablePlaces { get; set; } = [];
    public int? SelectedModeleId { get; set; }
    public int? SelectedPlaces { get; set; }
}
