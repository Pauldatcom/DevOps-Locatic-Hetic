public class Reservation
{
    public int Id {get; set;}
    public DateTime dateOfBegin {get; set;}
    public DateTime dateOfEnd {get; set;}

    public int clientId {get; set;}
    public required Client Client {get; set;}

    public int CarId {get; set;}
    public required Car Car {get; set;}

}