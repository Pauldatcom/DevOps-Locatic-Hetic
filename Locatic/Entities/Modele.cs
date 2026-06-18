public class Modele
{
    public int Id { get; set; }
    public required string ModeleName { get; set; }
    public int BrandId {get; set;}
    public required Brand Brand {get; set;}

}