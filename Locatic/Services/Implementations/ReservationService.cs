using Locatic.Data;
using Locatic.Entities;
using Locatic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Locatic.Services.Implementations;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _context;

    public ReservationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Reservation>> GetAllAsync()
    {
        return await _context.Reservations
            .Include(r => r.Client)
            .Include(r => r.Car)
                .ThenInclude(c => c.Modele)
                    .ThenInclude(m => m.Brand)
            .ToListAsync();
    }

    public async Task CreateAsync(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsCarAvailableAsync(int carId, DateTime startDate, DateTime endDate)
    {
        var hasConflict = await _context.Reservations.AnyAsync(r =>
            r.CarId == carId &&
            startDate <= r.EndDate &&
            endDate >= r.StartDate
        );

        return !hasConflict;
    }
}