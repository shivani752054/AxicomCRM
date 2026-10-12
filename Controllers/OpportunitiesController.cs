using System.Security.Claims; using AcxiomCRM.Data; using AcxiomCRM.Models; using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.Rendering; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize(Roles = "Admin,Manager,SalesExecutive")]
public class OpportunitiesController : Controller {
  readonly AppDbContext _db; readonly AuditService _a; public OpportunitiesController(AppDbContext db, AuditService a) { _db = db; _a = a; }
  string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  public async Task<IActionResult> Index(string? q, string? stage) {
    var x = _db.Opportunities.AsQueryable(); if (User.IsInRole("SalesExecutive")) x = x.Where(o => o.AssignedTo == Uid);
    if (!string.IsNullOrWhiteSpace(q)) x = x.Where(o => o.OpportunityName.Contains(q)); if (!string.IsNullOrEmpty(stage)) x = x.Where(o => o.Stage == stage);
    return View(await x.OrderByDescending(o => o.CreatedDate).Take(200).ToListAsync());
  }
  public async Task<IActionResult> Create() { await Cust(); return View(new Opportunity()); }
  [HttpPost] public async Task<IActionResult> Create(Opportunity m) {
    if (!await _db.Customers.AnyAsync(c => c.CustomerId == m.CustomerId)) ModelState.AddModelError("CustomerId", "Select a valid customer.");
    if (!ModelState.IsValid) { await Cust(); return View(m); }   // IValidatableObject rules run here on the server
    m.OpportunityId = 0; m.AssignedTo = Uid; m.Status = m.Stage == "Won" || m.Stage == "Lost" ? m.Stage : "Open";
    _db.Add(m); await _db.SaveChangesAsync(); await _a.Log("Create", "Opportunity", m.OpportunityId.ToString(), null, $"{m.OpportunityName}|{m.Amount}|{m.Stage}");
    return RedirectToAction(nameof(Index));
  }
  async Task Cust() => ViewBag.Customers = new SelectList(await _db.Customers.ToListAsync(), "CustomerId", "CustomerName");
}
