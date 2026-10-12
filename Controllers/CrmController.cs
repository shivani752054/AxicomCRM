using AcxiomCRM.Data;
using AcxiomCRM.Helpers;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public abstract class CrmController : Controller
{
    protected readonly ApplicationDbContext Db;
    protected readonly IAuditService Audit;

    protected CrmController(
        ApplicationDbContext db,
        IAuditService audit)
    {
        Db = db;
        Audit = audit;
    }

    protected string Uid => User.Uid();

    protected bool IsSales => User.IsInRole(AppRoles.SalesExecutive);

    protected async Task LoadUsersAsync()
    {
        ViewBag.Users = await Db.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.FullName
            })
            .ToListAsync();
    }

    protected async Task LoadCustomersAsync()
    {
        var q = Db.Customers.Where(c => c.Status == "Active");

        if (IsSales)
        {
            var id = Uid;
            q = q.Where(c => c.AssignedTo == id || c.CreatedBy == id);
        }

        ViewBag.Customers = await q
            .OrderBy(c => c.CustomerName)
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.CustomerName + " (" + c.CustomerCode + ")"
            })
            .ToListAsync();
    }

    protected async Task LoadLeadsAsync()
    {
        var q = Db.Leads.AsQueryable();

        if (IsSales)
        {
            var id = Uid;
            q = q.Where(l => l.AssignedTo == id);
        }

        ViewBag.Leads = await q
            .OrderBy(l => l.LeadName)
            .Select(l => new SelectListItem
            {
                Value = l.LeadId.ToString(),
                Text = l.LeadName + " (" + l.LeadCode + ")"
            })
            .ToListAsync();
    }

    /// <summary>
    /// Sales executives always own what they create.
    /// Other users may select an active assignee.
    /// </summary>
    protected async Task<string> ResolveAssigneeAsync(
        string? requested,
        string key)
    {
        if (IsSales)
            return Uid;

        if (string.IsNullOrWhiteSpace(requested))
        {
            ModelState.AddModelError(key, "Select a valid user.");
            return Uid;
        }

        var exists = await Db.Users
            .AnyAsync(u => u.Id == requested && u.IsActive);

        if (!exists)
        {
            ModelState.AddModelError(key, "Select a valid user.");
            return Uid;
        }

        return requested;
    }
}