using Locatic.Data;
using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Services.Implementations;

public class ModeleService : IModeleService
{
    private readonly AppDbContext _context;

    public ModeleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Modele>> GetAllAsync()
    {
        return await _context.Modeles
            .Include(m => m.Brand)
            .ToListAsync();
    }

    public async Task<Modele?> GetByIdAsync(int id)
    {
        return await _context.Modeles
            .Include(m => m.Brand)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task CreateAsync(Modele modele)
    {
        _context.Modeles.Add(modele);
        await _context.SaveChangesAsync();
    }
}