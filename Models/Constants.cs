namespace AcxiomCRM.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
    public const string AdminOrManager = "Admin,Manager";
    public static readonly string[] All = { Admin, Manager, SalesExecutive };
}

public static class Rules
{
    public const string PhonePattern = @"^[6-9]\d{9}$";
    public const string PhoneMessage = "Enter a valid phone number.";
    public const string EmailMessage = "Enter a valid email address.";
}

public static class LeadStatuses
{
    public const string New = "New", Contacted = "Contacted", Qualified = "Qualified",
        Unqualified = "Unqualified", Converted = "Converted", Lost = "Lost";
    public static readonly string[] All = { New, Contacted, Qualified, Unqualified, Converted, Lost };

    // Configured workflow: allowed status transitions
    static readonly Dictionary<string, string[]> Map = new()
    {
        [New] = new[] { Contacted, Unqualified, Lost },
        [Contacted] = new[] { Qualified, Unqualified, Lost },
        [Qualified] = new[] { Contacted, Converted, Lost },
        [Unqualified] = new[] { Contacted, Lost },
        [Converted] = Array.Empty<string>(),
        [Lost] = new[] { New }
    };
    public static string[] Next(string from) => Map.TryGetValue(from ?? "", out var n) ? n : Array.Empty<string>();
    public static bool CanMove(string from, string to) => from == to || Next(from).Contains(to);
}

public static class Stages
{
    public const string Qualification = "Qualification", Proposal = "Proposal",
        Negotiation = "Negotiation", Won = "Won", Lost = "Lost";
    public static readonly string[] All = { Qualification, Proposal, Negotiation, Won, Lost };
    public static bool IsActive(string stage) => stage != Won && stage != Lost;
}

public static class FollowUpStatuses
{
    public const string Planned = "Planned", Completed = "Completed", Missed = "Missed", Cancelled = "Cancelled";
    public static readonly string[] All = { Planned, Completed, Missed, Cancelled };
}

public static class ActivityTypes
{
    public static readonly string[] All = { "Call", "Meeting", "Email", "Task" };
    public static readonly string[] Statuses = { "Planned", "Completed", "Cancelled" };
}
