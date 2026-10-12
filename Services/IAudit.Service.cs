namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string action,
            string entity,
            string? id = null,
            object? oldV = null,
            object? newV = null,
            string? userId = null);

        Task Log(
            string action,
            string entity,
            string? id = null,
            object? oldV = null,
            object? newV = null,
            string? userId = null);

        string Snapshot(object? value);
    }
}