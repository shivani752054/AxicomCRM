using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Helpers;

public static class UserExtensions
{
    public static string Uid(this ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier);
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int Total { get; set; }

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> q, int page, int size = 10)
    {
        page = Math.Max(1, page);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return new PagedResult<T> { Items = items, Page = page, Total = total, TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size)) };
    }
}

public static class Codes
{
    public static string New(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
}
