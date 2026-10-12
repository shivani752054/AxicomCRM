using System.Security.Claims;
using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            AppDbContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string action,
            string entity,
            string? id = null,
            object? oldV = null,
            object? newV = null,
            string? userId = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                UserId = userId ??
                    httpContext?.User.FindFirstValue(
                        ClaimTypes.NameIdentifier),

                Action = action,
                EntityName = entity,
                RecordId = id,
                OldValue = Snapshot(oldV),
                NewValue = Snapshot(newV),

                IpAddress = httpContext?.Connection
                    .RemoteIpAddress?.ToString(),

                CreatedDate = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);

            await _db.SaveChangesAsync();
        }

        public Task Log(
            string action,
            string entity,
            string? id = null,
            object? oldV = null,
            object? newV = null,
            string? userId = null)
        {
            return LogAsync(
                action,
                entity,
                id,
                oldV,
                newV,
                userId);
        }

        public string Snapshot(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return text;
            }

            return JsonSerializer.Serialize(value);
        }
    }
}