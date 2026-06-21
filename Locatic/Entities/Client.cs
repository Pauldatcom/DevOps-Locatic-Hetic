public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string FirstName { get; set; }
    public required string Email { get; set; }
    public required string Telephone { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}