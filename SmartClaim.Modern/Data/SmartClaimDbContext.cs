using Microsoft.EntityFrameworkCore;

namespace SmartClaim.Modern.Data;

public class SmartClaimDbContext : DbContext
{
    public SmartClaimDbContext(DbContextOptions<SmartClaimDbContext> options) : base(options)
    {
    }

    public DbSet<ClaimRecord> Claims => Set<ClaimRecord>();
}
