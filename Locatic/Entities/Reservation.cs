public class Reservation
{
    public int Id { get; set; }
    public DateTime DateOfBegin { get; set; }
    public DateTime DateOfEnd { get; set; }

    public int ClientId { get; set; }
    public required Client Client { get; set; }

    public int CarId { get; set; }
    public required Car Car { get; set; }

}