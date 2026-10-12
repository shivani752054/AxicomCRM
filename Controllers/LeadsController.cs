using AcxiomCRM.Data;
using AcxiomCRM.Helpers;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class LeadsController : CrmController
{
    const string Bind_ = "LeadName,Email,Phone,CompanyName,Source,Status,ExpectedValue,Priority,Notes,AssignedTo";
    public LeadsController(ApplicationDbContext db, IAuditService a) : base(db, a) { }

    IQueryable<Lead> Scoped()
    {
        var q = Db.Leads.AsQueryable();
        if (IsSales) { var id = Uid; q = q.Where(l => l.AssignedTo == id); }
        return q;
    }

    public async Task<IActionResult> Index(string q, string status, string assignedTo, int page = 1)
    {
        var query = Scoped().Include(l => l.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(l => l.LeadName.Contains(q) || l.CompanyName.Contains(q));
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
        if (!string.IsNullOrEmpty(assignedTo)) query = query.Where(l => l.AssignedTo == assignedTo);
        ViewBag.Q = q; ViewBag.Status = status; ViewBag.AssignedTo = assignedTo;
        await LoadUsersAsync();
        return View(await PagedResult<Lead>.CreateAsync(query.OrderByDescending(l => l.CreatedDate), page));
    }

    public async Task<IActionResult> Details(int id)
    {
        var l = await Scoped().Include(x => x.AssignedUser).FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return NotFound();
        ViewBag.FollowUps = await Db.FollowUps.Where(f => f.LeadId == id).OrderByDescending(f => f.FollowUpDate).ToListAsync();
        return View(l);
    }

    public async Task<IActionResult> Create() { await LoadUsersAsync(); return View(new Lead { AssignedTo = Uid }); }

    [HttpPost]
    public async Task<IActionResult> Create([Bind(Bind_)] Lead m)
    {
        m.AssignedTo = await ResolveAssigneeAsync(m.AssignedTo, nameof(m.AssignedTo));
        if (!LeadStatuses.All.Contains(m.Status) || m.Status == LeadStatuses.Converted)
            ModelState.AddModelError(nameof(m.Status), "Select a valid lead status.");
        if (!ModelState.IsValid) { await LoadUsersAsync(); return View(m); }
        m.LeadCode = Codes.New("LEAD"); m.CreatedDate = DateTime.Now;
        Db.Leads.Add(m); await Db.SaveChangesAsync();
        await Audit.LogAsync("Create", "Lead", m.LeadId.ToString(), null, m);
        TempData["Success"] = "Lead created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var l = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return NotFound();
        if (l.Status == LeadStatuses.Converted) { TempData["Error"] = "Converted leads cannot be edited."; return RedirectToAction(nameof(Index)); }
        await LoadUsersAsync(); return View(l);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(Bind_)] Lead m)
    {
        var l = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return NotFound();
        if (l.Status == LeadStatuses.Converted) return Forbid();
        m.AssignedTo = IsSales ? l.AssignedTo : await ResolveAssigneeAsync(m.AssignedTo, nameof(m.AssignedTo));
        if (!LeadStatuses.All.Contains(m.Status)) ModelState.AddModelError(nameof(m.Status), "Select a valid lead status.");
        else if (m.Status == LeadStatuses.Converted) ModelState.AddModelError(nameof(m.Status), "Use the Convert action to convert a lead.");
        else if (!LeadStatuses.CanMove(l.Status, m.Status))
            ModelState.AddModelError(nameof(m.Status), $"Invalid status transition: {l.Status} → {m.Status}.");
        if (!ModelState.IsValid) { m.LeadId = id; ViewBag.OldStatus = l.Status; await LoadUsersAsync(); return View(m); }
        var old = Audit.Snapshot(l);
        l.LeadName = m.LeadName; l.Email = m.Email; l.Phone = m.Phone; l.CompanyName = m.CompanyName; l.Source = m.Source;
        l.Status = m.Status; l.ExpectedValue = m.ExpectedValue; l.Priority = m.Priority; l.Notes = m.Notes; l.AssignedTo = m.AssignedTo;
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Update", "Lead", id.ToString(), old, l);
        TempData["Success"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, string status)
    {
        var l = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return NotFound();
        if (status == LeadStatuses.Converted || !LeadStatuses.All.Contains(status) || !LeadStatuses.CanMove(l.Status, status))
        { TempData["Error"] = $"Invalid status transition: {l.Status} → {status}."; return RedirectToAction(nameof(Details), new { id }); }
        var old = Audit.Snapshot(l); l.Status = status;
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Status Change", "Lead", id.ToString(), old, l);
        TempData["Success"] = "Lead status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Convert(int id)
    {
        var l = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return NotFound();
        if (l.Status != LeadStatuses.Qualified) { TempData["Error"] = "Only Qualified leads can be converted."; return RedirectToAction(nameof(Details), new { id }); }
        if (l.ExpectedValue <= 0) { TempData["Error"] = "Set an Expected Value greater than 0 before converting."; return RedirectToAction(nameof(Details), new { id }); }

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.Email == l.Email || c.Phone == l.Phone);
        if (customer == null)
        {
            customer = new Customer { CustomerCode = Codes.New("CUST"), CustomerName = l.LeadName, Email = l.Email, Phone = l.Phone,
                CompanyName = l.CompanyName, Status = "Active", CreatedBy = Uid, AssignedTo = l.AssignedTo, CreatedDate = DateTime.Now };
            Db.Customers.Add(customer);
            await Db.SaveChangesAsync();
            await Audit.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null, customer);
        }
        var opp = new Opportunity { OpportunityName = (l.CompanyName ?? l.LeadName) + " - Opportunity", CustomerId = customer.CustomerId, LeadId = l.LeadId,
            Amount = l.ExpectedValue, Stage = Stages.Qualification, Probability = 20, ExpectedCloseDate = DateTime.Today.AddDays(30),
            Status = "Open", AssignedTo = l.AssignedTo, CreatedDate = DateTime.Now };
        Db.Opportunities.Add(opp);
        var old = Audit.Snapshot(l); l.Status = LeadStatuses.Converted;
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Convert", "Lead", id.ToString(), old, new { l.Status, CustomerId = customer.CustomerId, OpportunityId = opp.OpportunityId });
        TempData["Success"] = "Lead converted to customer and opportunity.";
        return RedirectToAction("Details", "Opportunities", new { id = opp.OpportunityId });
    }

    [HttpPost, Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await Db.Leads.FindAsync(id);
        if (l == null) return NotFound();
        var old = Audit.Snapshot(l);
        foreach (var o in Db.Opportunities.Where(o => o.LeadId == id)) o.LeadId = null;
        Db.FollowUps.RemoveRange(Db.FollowUps.Where(f => f.LeadId == id));
        Db.Activities.RemoveRange(Db.Activities.Where(a => a.LeadId == id));
        Db.Leads.Remove(l);
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Delete", "Lead", id.ToString(), old, null);
        TempData["Success"] = "Lead deleted.";
        return RedirectToAction(nameof(Index));
    }
}
