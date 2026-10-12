using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Opportunity> Opportunities { get; set; }
    public DbSet<FollowUp> FollowUps { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        // Uniqueness enforced at database level too
        b.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        b.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
        b.Entity<Customer>().HasIndex(c => c.CustomerCode).IsUnique();
        b.Entity<Lead>().HasIndex(l => l.LeadCode).IsUnique();
        b.Entity<AuditLog>().HasIndex(a => a.CreatedDate);

        // No cascading deletes between CRM tables (avoids SQL Server multiple-cascade-path errors)
        foreach (var fk in b.Model.GetEntityTypes()
                     .Where(t => t.ClrType.Namespace == "AcxiomCRM.Models" && t.ClrType != typeof(AppUser))
                     .SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    // Audit log is append-only: updates/deletes are rejected
    void GuardAudit()
    {
        if (ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Modified || e.State == EntityState.Deleted))
            throw new InvalidOperationException("Audit records are append-only and cannot be modified or deleted.");
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { GuardAudit(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    { GuardAudit(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
