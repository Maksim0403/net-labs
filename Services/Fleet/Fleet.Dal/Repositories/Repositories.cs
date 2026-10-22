using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Fleet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Dal.Repositories;

public interface IGenericRepository<T> where T : class
{ Task<T?> GetByIdAsync(long id, CancellationToken ct = default); Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default); Task<T> AddAsync(T entity, CancellationToken ct = default); void Update(T entity); void Delete(T entity); Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default); Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default); }
public class GenericRepository<T>(FleetDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly FleetDbContext Context = context; protected DbSet<T> Set => Context.Set<T>();
    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default) => await Set.FindAsync([id], ct);
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) => await Set.AsNoTracking().ToListAsync(ct);
    public async Task<T> AddAsync(T entity, CancellationToken ct = default) { await Set.AddAsync(entity, ct); return entity; }
    public void Update(T entity) => Set.Update(entity); public void Delete(T entity) => Set.Remove(entity);
    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default) => await Set.WithSpecification(spec).ToListAsync(ct);
    public Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default) => Set.WithSpecification(spec).CountAsync(ct);
}
public interface ICourierRepository : IGenericRepository<Courier> { Task<Courier?> GetWithDetailsAsync(long id, CancellationToken ct = default); Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default); }
public sealed class CourierRepository(FleetDbContext context) : GenericRepository<Courier>(context), ICourierRepository
{ public Task<Courier?> GetWithDetailsAsync(long id, CancellationToken ct = default) => Set.Include(x => x.License).Include(x => x.Warehouse).Include(x => x.CourierZones).ThenInclude(x => x.Zone).FirstOrDefaultAsync(x => x.Id == id, ct); public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default) => Set.AnyAsync(x => x.Phone == phone, ct); }
public interface IWarehouseRepository : IGenericRepository<Warehouse> { Task<Warehouse?> GetWithVehiclesAsync(long id, CancellationToken ct = default); }
public sealed class WarehouseRepository(FleetDbContext context) : GenericRepository<Warehouse>(context), IWarehouseRepository
{ public async Task<Warehouse?> GetWithVehiclesAsync(long id, CancellationToken ct = default) { var warehouse = await Set.FirstOrDefaultAsync(x => x.Id == id, ct); if (warehouse is not null) await Context.Entry(warehouse).Collection(x => x.Vehicles).LoadAsync(ct); return warehouse; } }
public interface ICourierZoneRepository { Task<IReadOnlyList<Courier>> GetCouriersAssignedSinceAsync(string zoneName, DateTime since, CancellationToken ct = default); }
public sealed class CourierZoneRepository(FleetDbContext context) : ICourierZoneRepository
{ public async Task<IReadOnlyList<Courier>> GetCouriersAssignedSinceAsync(string zoneName, DateTime since, CancellationToken ct = default) => await context.Set<CourierZone>().Where(x => x.Zone.Name == zoneName && x.AssignedAt >= since).OrderByDescending(x => x.AssignedAt).Select(x => x.Courier).AsNoTracking().ToListAsync(ct); }
