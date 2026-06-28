using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities;

public class Brand
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Country { get; set; }

    public ICollection<Modele> Modeles { get; set; } = new List<Modele>();
}