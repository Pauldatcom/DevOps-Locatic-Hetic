using Locatic.Data;
using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Services.Implementations;

public class BrandService : IBrandService
{
    private readonly AppDbContext _context;

    public BrandService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Brand>> GetAllAsync()
    {
        return await _context.Brands.ToListAsync();
    }

    public async Task<Brand?> GetByIdAsync(int id)
    {
        return await _context.Brands.FindAsync(id);
    }

    public async Task CreateAsync(Brand brand)
    {
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
    }
}