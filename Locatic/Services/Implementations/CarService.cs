using Locatic.Data;
using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Services.Implementations;

public class CarService : ICarService
{
    private readonly AppDbContext _context;

    public CarService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Car>> GetAllAsync()
    {
        return await _context.Cars
            .Include(c => c.Modele)
            .ThenInclude(m => m.Brand)
            .ToListAsync();
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await _context.Cars
            .Include(c => c.Modele)
            .ThenInclude(m => m.Brand)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task CreateAsync(Car car)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Car car)
    {
        _context.Cars.Update(car);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return;
        }

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
    }
}