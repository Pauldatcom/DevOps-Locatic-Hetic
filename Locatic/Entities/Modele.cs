public class Modele
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BrandId { get; set; }
    public required Brand Brand { get; set; }

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}