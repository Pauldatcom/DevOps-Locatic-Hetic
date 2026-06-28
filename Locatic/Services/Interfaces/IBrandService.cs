using Locatic.Entities;

namespace Locatic.Services.Interfaces;

public interface IBrandService
{
    Task<IEnumerable<Brand>> GetAllAsync();
    Task<Brand?> GetByIdAsync(int id);
    Task CreateAsync(Brand brand);
}