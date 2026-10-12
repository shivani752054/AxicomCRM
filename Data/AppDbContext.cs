using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        builder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();

        builder.Entity<Opportunity>()
            .Property(o => o.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Lead>()
            .Property(l => l.ExpectedValue)
            .HasPrecision(18, 2);
    }
}