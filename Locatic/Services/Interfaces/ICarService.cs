using Locatic.Entities;

namespace Locatic.Services.Interfaces;

public interface ICarService
{
    Task<IEnumerable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task CreateAsync(Car car);
    Task UpdateAsync(Car car);
    Task DeleteAsync(int id);
}