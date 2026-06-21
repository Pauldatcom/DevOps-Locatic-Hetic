public class Car 
{
    public int Id { get; set; }
    public required string Immatriculation { get; set; }
    public int Year { get; set; }
    public int NumberOfPlaces { get; set; }
    public decimal TarifPerDay { get; set; }
    public required string TypeOfFuel { get; set; }

    public int ModeleId { get; set; }
    public required Modele Modele { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}