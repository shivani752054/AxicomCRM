using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();

        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<AppUser>>();
        foreach (var r in AppRoles.All)
            if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

        async Task<AppUser> Make(string name, string email, string pwd, string role)
        {
            var u = await um.FindByEmailAsync(email);
            if (u == null)
            {
                u = new AppUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
                var res = await um.CreateAsync(u, pwd);
                if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors.Select(e => e.Description)));
                await um.AddToRoleAsync(u, role);
            }
            return u;
        }

        var admin = await Make("System Admin", "admin@acxiom.com", "Admin@123", AppRoles.Admin);
        var mgr = await Make("Maya Manager", "manager@acxiom.com", "Manager@123", AppRoles.Manager);
        var sales = await Make("Sam Sales", "sales@acxiom.com", "Sales@123", AppRoles.SalesExecutive);
        var sales2 = await Make("Sara Sales", "sales2@acxiom.com", "Sales@123", AppRoles.SalesExecutive);

        if (await db.Customers.AnyAsync()) return;

        var c1 = new Customer { CustomerCode = "CUST-0001", CustomerName = "Ravi Kumar", Email = "ravi@infotech.com", Phone = "9876543210", CompanyName = "InfoTech Pvt Ltd", City = "Kolkata", State = "West Bengal", AssignedTo = sales.Id, CreatedBy = admin.Id };
        var c2 = new Customer { CustomerCode = "CUST-0002", CustomerName = "Anita Sen", Email = "anita@globalsoft.com", Phone = "9123456780", CompanyName = "GlobalSoft", City = "Mumbai", State = "Maharashtra", AssignedTo = sales2.Id, CreatedBy = admin.Id };
        var c3 = new Customer { CustomerCode = "CUST-0003", CustomerName = "Imran Khan", Email = "imran@retailhub.com", Phone = "8899776655", CompanyName = "RetailHub", City = "Delhi", State = "Delhi", AssignedTo = sales.Id, CreatedBy = admin.Id };
        db.Customers.AddRange(c1, c2, c3);

        db.Leads.AddRange(
            new Lead { LeadCode = "LEAD-0001", LeadName = "Priya Das", Email = "priya@startup.io", Phone = "9000011111", CompanyName = "Startup IO", Source = "Website", Status = LeadStatuses.New, ExpectedValue = 150000, Priority = "High", AssignedTo = sales.Id },
            new Lead { LeadCode = "LEAD-0002", LeadName = "John Paul", Email = "john@acme.com", Phone = "9000022222", CompanyName = "Acme Corp", Source = "Referral", Status = LeadStatuses.Qualified, ExpectedValue = 400000, Priority = "Medium", AssignedTo = sales.Id },
            new Lead { LeadCode = "LEAD-0003", LeadName = "Neha Roy", Email = "neha@bizz.com", Phone = "9000033333", CompanyName = "Bizz Ltd", Source = "Cold Call", Status = LeadStatuses.Contacted, ExpectedValue = 90000, Priority = "Low", AssignedTo = sales2.Id });
        await db.SaveChangesAsync();

        db.Opportunities.AddRange(
            new Opportunity { OpportunityName = "ERP Rollout", CustomerId = c1.CustomerId, Amount = 500000, Stage = Stages.Proposal, Probability = 50, ExpectedCloseDate = DateTime.Today.AddDays(30), AssignedTo = sales.Id },
            new Opportunity { OpportunityName = "CRM License", CustomerId = c2.CustomerId, Amount = 250000, Stage = Stages.Negotiation, Probability = 75, ExpectedCloseDate = DateTime.Today.AddDays(15), AssignedTo = sales2.Id },
            new Opportunity { OpportunityName = "Support Contract", CustomerId = c3.CustomerId, Amount = 120000, Stage = Stages.Won, Probability = 100, Status = "Won", ClosedDate = DateTime.Today.AddDays(-5), ExpectedCloseDate = DateTime.Today.AddDays(-5), AssignedTo = sales.Id },
            new Opportunity { OpportunityName = "Cloud Migration", CustomerId = c1.CustomerId, Amount = 80000, Stage = Stages.Lost, Probability = 0, Status = "Lost", ClosedDate = DateTime.Today.AddDays(-20), ExpectedCloseDate = DateTime.Today.AddDays(-20), AssignedTo = sales.Id });
        db.FollowUps.AddRange(
            new FollowUp { CustomerId = c1.CustomerId, FollowUpDate = DateTime.Today.AddDays(1).AddHours(10), Subject = "Demo call", FollowUpType = "Call", AssignedTo = sales.Id },
            new FollowUp { CustomerId = c2.CustomerId, FollowUpDate = DateTime.Today.AddDays(3).AddHours(15), Subject = "Pricing meeting", FollowUpType = "Meeting", AssignedTo = sales2.Id });
        db.Activities.Add(new Activity { ActivityType = "Call", Subject = "Intro call", ActivityDate = DateTime.Today, CustomerId = c1.CustomerId, AssignedTo = sales.Id, Status = "Completed" });
        await db.SaveChangesAsync();
    }
}
