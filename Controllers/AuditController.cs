using AcxiomCRM.Data; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize(Roles = "Admin")]
public class AuditController : Controller {
  readonly AppDbContext _db; public AuditController(AppDbContext db) => _db = db;
  public async Task<IActionResult> Index(string? action) {
    var x = _db.AuditLogs.AsQueryable(); if (!string.IsNullOrEmpty(action)) x = x.Where(a => a.Action == action);
    return View(await x.OrderByDescending(a => a.CreatedDate).Take(300).ToListAsync());
  }
}
