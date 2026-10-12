using System.Security.Claims; using AcxiomCRM.Data; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize]
public class HomeController : Controller {
  readonly AppDbContext _db; public HomeController(AppDbContext db) => _db = db;
  public async Task<IActionResult> Index() {
    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier); bool own = User.IsInRole("SalesExecutive");
    var cs = _db.Customers.Where(c => !own || c.CreatedBy == uid);
    var ls = _db.Leads.Where(l => !own || l.AssignedTo == uid);
    var os = _db.Opportunities.Where(o => !own || o.AssignedTo == uid);
    ViewBag.Cards = new Dictionary<string, object> {
      ["Total Customers"] = await cs.CountAsync(), ["Total Leads"] = await ls.CountAsync(),
      ["Open Leads"] = await ls.CountAsync(l => l.Status != "Lost" && l.Status != "Converted" && l.Status != "Unqualified"),
      ["Total Opportunities"] = await os.CountAsync(), ["Open Opportunities"] = await os.CountAsync(o => o.Stage != "Won" && o.Stage != "Lost"),
      ["Won"] = await os.CountAsync(o => o.Stage == "Won"), ["Lost"] = await os.CountAsync(o => o.Stage == "Lost"),
      ["Pipeline Value"] = (await os.Where(o => o.Stage != "Won" && o.Stage != "Lost").SumAsync(o => (decimal?)o.Amount) ?? 0).ToString("N0") };
    var lead = await ls.GroupBy(l => l.Status).Select(g => new { k = g.Key, v = g.Count() }).ToListAsync();
    var pipe = await os.GroupBy(o => o.Stage).Select(g => new { k = g.Key, v = g.Count() }).ToListAsync();
    var monthly = await os.Where(o => o.Stage == "Won").GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month })
      .Select(g => new { k = g.Key.Year + "-" + g.Key.Month, v = g.Sum(x => x.Amount) }).ToListAsync();
    ViewBag.Lead = lead; ViewBag.Pipe = pipe; ViewBag.Monthly = monthly; return View();
  }
}
