using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities;

public class Modele
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}