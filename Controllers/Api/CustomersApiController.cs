using System.Security.Claims; using AcxiomCRM.Data; using AcxiomCRM.Models; using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers.Api;
[ApiController, Route("api/customers"), Authorize(Roles = "Admin,Manager,SalesExecutive"), IgnoreAntiforgeryToken]
public class CustomersApiController : ControllerBase {
  readonly AppDbContext _db; readonly AuditService _a; public CustomersApiController(AppDbContext db, AuditService a) { _db = db; _a = a; }
  string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
  IQueryable<Customer> Scope() => User.IsInRole("SalesExecutive") ? _db.Customers.Where(c => c.CreatedBy == Uid) : _db.Customers;
  static CustomerDto D(Customer c) => new(c.CustomerId, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.Status);
  [HttpGet] public async Task<ActionResult<IEnumerable<CustomerDto>>> Get() => Ok((await Scope().ToListAsync()).Select(D));
  [HttpGet("{id}")] public async Task<ActionResult<CustomerDto>> Get(int id) { var c = await Scope().FirstOrDefaultAsync(x => x.CustomerId == id); return c == null ? NotFound() : Ok(D(c)); }
  [HttpPost] public async Task<ActionResult<CustomerDto>> Post(Customer m) {
    if (await _db.Customers.AnyAsync(c => c.Email == m.Email || c.Phone == m.Phone)) return Conflict(new { error = "Duplicate customer." });
    m.CustomerId = 0; m.CreatedBy = Uid; m.CustomerCode = "CUS" + Guid.NewGuid().ToString("N")[..6].ToUpper();
    _db.Add(m); await _db.SaveChangesAsync(); await _a.Log("Create", "Customer", m.CustomerId.ToString(), null, "API");
    return CreatedAtAction(nameof(Get), new { id = m.CustomerId }, D(m));
  }
  [HttpPut("{id}")] public async Task<IActionResult> Put(int id, Customer m) {
    var c = await Scope().FirstOrDefaultAsync(x => x.CustomerId == id); if (c == null) return NotFound();
    c.CustomerName = m.CustomerName; c.Email = m.Email; c.Phone = m.Phone; c.CompanyName = m.CompanyName; c.Status = m.Status;
    await _db.SaveChangesAsync(); await _a.Log("Update", "Customer", id.ToString(), null, "API"); return NoContent();
  }
  [HttpDelete("{id}"), Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Delete(int id) {
    var c = await _db.Customers.FindAsync(id); if (c == null) return NotFound(); c.Status = "Inactive"; await _db.SaveChangesAsync(); await _a.Log("Delete", "Customer", id.ToString()); return NoContent();
  }
}
