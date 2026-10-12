using System.Security.Claims; using System.Text.Json; using AcxiomCRM.Data; using AcxiomCRM.Models; using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize(Roles = "Admin,Manager,SalesExecutive")]
public class CustomersController : Controller {
  readonly AppDbContext _db; readonly AuditService _a; public CustomersController(AppDbContext db, AuditService a) { _db = db; _a = a; }
  string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  IQueryable<Customer> Scope() => User.IsInRole("SalesExecutive") ? _db.Customers.Where(c => c.CreatedBy == Uid) : _db.Customers;
  public async Task<IActionResult> Index(string? q) {
    var x = Scope(); if (!string.IsNullOrWhiteSpace(q)) x = x.Where(c => c.CustomerName.Contains(q) || c.Email.Contains(q) || c.Phone.Contains(q) || (c.CompanyName ?? "").Contains(q));
    ViewBag.Q = q; return View(await x.OrderByDescending(c => c.CreatedDate).Take(200).ToListAsync());
  }
  public IActionResult Create() => View("Form", new Customer());
  [HttpPost] public async Task<IActionResult> Create(Customer m) {
    await Dups(m); if (!ModelState.IsValid) return View("Form", m);
    m.CreatedBy = Uid; m.CustomerCode = "CUS" + Guid.NewGuid().ToString("N")[..6].ToUpper(); m.CustomerId = 0;
    _db.Add(m); await _db.SaveChangesAsync(); await _a.Log("Create", "Customer", m.CustomerId.ToString(), null, JsonSerializer.Serialize(m));
    TempData["ok"] = "Customer created."; return RedirectToAction(nameof(Index));
  }
  public async Task<IActionResult> Edit(int id) { var c = await Scope().FirstOrDefaultAsync(x => x.CustomerId == id); return c == null ? NotFound() : View("Form", c); }
  [HttpPost] public async Task<IActionResult> Edit(int id, Customer m) {
    var c = await Scope().FirstOrDefaultAsync(x => x.CustomerId == id); if (c == null) return NotFound();  // blocks tampering with other users' records
    m.CustomerId = id; await Dups(m); if (!ModelState.IsValid) return View("Form", m);
    var old = JsonSerializer.Serialize(c);
    c.CustomerName = m.CustomerName; c.Email = m.Email; c.Phone = m.Phone; c.CompanyName = m.CompanyName; c.Address = m.Address; c.City = m.City; c.State = m.State; c.Status = m.Status;
    await _db.SaveChangesAsync(); await _a.Log("Update", "Customer", id.ToString(), old, JsonSerializer.Serialize(c)); return RedirectToAction(nameof(Index));
  }
  [HttpPost, Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Delete(int id) {   // soft delete
    var c = await _db.Customers.FindAsync(id); if (c == null) return NotFound();
    c.Status = "Inactive"; await _db.SaveChangesAsync(); await _a.Log("Delete", "Customer", id.ToString(), "Active", "Inactive"); return RedirectToAction(nameof(Index));
  }
  async Task Dups(Customer m) {
    if (await _db.Customers.AnyAsync(c => c.Email == m.Email && c.CustomerId != m.CustomerId)) ModelState.AddModelError("Email", "Email already exists.");
    if (await _db.Customers.AnyAsync(c => c.Phone == m.Phone && c.CustomerId != m.CustomerId)) ModelState.AddModelError("Phone", "Phone already exists.");
  }
}
