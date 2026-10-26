using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fleet.Dal;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args)
    { var options = new DbContextOptionsBuilder<FleetDbContext>().UseSqlServer("Server=.\\SQLEXPRESS;Database=FleetEfDb;Trusted_Connection=True;TrustServerCertificate=True").Options; return new FleetDbContext(options); }
}
