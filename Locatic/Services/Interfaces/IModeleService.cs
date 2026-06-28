using Locatic.Entities;

namespace Locatic.Services.Interfaces;

public interface IModeleService
{
    Task<IEnumerable<Modele>> GetAllAsync();
    Task<Modele?> GetByIdAsync(int id);
    Task CreateAsync(Modele modele);
}