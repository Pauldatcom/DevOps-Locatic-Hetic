using System.Diagnostics.CodeAnalysis;

public class Car 
{
    public int Id {get; set;}
    public int immatriculation {get; set;}
    public int year {get; set;}
    public int numberOfPlaces {get; set;}
    public int tarifPerDay {get; set;}
    public required string typeOfFuel {get; set;}

    public int ModeleId {get; set;}
    public required Modele Modele {get; set;}

}