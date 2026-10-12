using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class DashboardController : CrmController
{
    public DashboardController(ApplicationDbContext db, IAuditService a) : base(db, a) { }

    public async Task<IActionResult> Index(string range = "all", DateTime? from = null, DateTime? to = null)
    {
        DateTime? f = null, t = null; var today = DateTime.Today;
        switch (range)
        {
            case "today": f = today; t = today; break;
            case "week": f = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); t = today; break;
            case "month": f = new DateTime(today.Year, today.Month, 1); t = today; break;
            case "custom": f = from; t = to; break;
        }
        var uid = Uid; var sales = IsSales;
        var cq = Db.Customers.AsQueryable(); var lq = Db.Leads.AsQueryable();
        var oq = Db.Opportunities.AsQueryable(); var fq = Db.FollowUps.AsQueryable();
        // Server-side authorization of dashboard data
        if (sales)
        {
            cq = cq.Where(c => c.AssignedTo == uid || c.CreatedBy == uid);
            lq = lq.Where(l => l.AssignedTo == uid);
            oq = oq.Where(o => o.AssignedTo == uid);
            fq = fq.Where(x => x.AssignedTo == uid);
        }
        var monthlyBase = oq.Where(o => o.Stage == Stages.Won && o.ClosedDate != null);   // not date-filtered
        if (f.HasValue) { var fd = f.Value.Date; cq = cq.Where(c => c.CreatedDate >= fd); lq = lq.Where(l => l.CreatedDate >= fd); oq = oq.Where(o => o.CreatedDate >= fd); }
        if (t.HasValue) { var td = t.Value.Date.AddDays(1); cq = cq.Where(c => c.CreatedDate < td); lq = lq.Where(l => l.CreatedDate < td); oq = oq.Where(o => o.CreatedDate < td); }

        var vm = new DashboardVM { Range = range, From = f, To = t };
        vm.TotalCustomers = await cq.CountAsync();
        vm.TotalLeads = await lq.CountAsync();
        vm.OpenLeads = await lq.CountAsync(l => l.Status != LeadStatuses.Lost && l.Status != LeadStatuses.Converted && l.Status != LeadStatuses.Unqualified);
        vm.TotalOpportunities = await oq.CountAsync();
        vm.OpenOpportunities = await oq.CountAsync(o => o.Status == "Open");
        vm.WonOpportunities = await oq.CountAsync(o => o.Stage == Stages.Won);
        vm.LostOpportunities = await oq.CountAsync(o => o.Stage == Stages.Lost);
        vm.PendingFollowUps = await fq.CountAsync(x => x.Status == FollowUpStatuses.Planned);
        vm.PipelineValue = await oq.Where(o => o.Status == "Open").SumAsync(o => o.Amount);
        vm.WeightedPipeline = await oq.Where(o => o.Status == "Open").SumAsync(o => o.Amount * o.Probability / 100m);

        var leadGroups = await lq.GroupBy(l => l.Status).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        foreach (var s in new[] { LeadStatuses.New, LeadStatuses.Contacted, LeadStatuses.Qualified, LeadStatuses.Lost, LeadStatuses.Converted })
        { vm.LeadLabels.Add(s); vm.LeadCounts.Add(leadGroups.FirstOrDefault(g => g.Key == s)?.N ?? 0); }

        var oppGroups = await oq.GroupBy(o => o.Stage).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        foreach (var s in Stages.All)
        { vm.OppLabels.Add(s); vm.OppCounts.Add(oppGroups.FirstOrDefault(g => g.Key == s)?.N ?? 0); }

        var start = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var won = await monthlyBase.Where(o => o.ClosedDate >= start).Select(o => new { o.ClosedDate, o.Amount }).ToListAsync();
        for (int i = 0; i < 6; i++)
        {
            var m = start.AddMonths(i);
            vm.MonthLabels.Add(m.ToString("MMM yyyy"));
            vm.MonthTotals.Add(won.Where(w => w.ClosedDate.Value.Year == m.Year && w.ClosedDate.Value.Month == m.Month).Sum(w => w.Amount));
        }
        vm.Upcoming = await fq.Where(x => x.Status == FollowUpStatuses.Planned).Include(x => x.Customer).Include(x => x.Lead)
            .OrderBy(x => x.FollowUpDate).Take(5).ToListAsync();
        return View(vm);
    }
}
