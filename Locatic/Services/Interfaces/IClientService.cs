using Locatic.Entities;

namespace Locatic.Services.Interfaces;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(int id);
    Task CreateAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);
}