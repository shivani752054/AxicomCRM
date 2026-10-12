using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Email is required."), Display(Name = "Email / Username")] public string Email { get; set; }
    [Required(ErrorMessage = "Password is required."), DataType(DataType.Password)] public string Password { get; set; }
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [Required(ErrorMessage = "Full Name is required."), StringLength(100), Display(Name = "Full Name")] public string FullName { get; set; }
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = Rules.EmailMessage), StringLength(150)] public string Email { get; set; }
    [Required(ErrorMessage = "Password is required."), DataType(DataType.Password),
     StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] public string Password { get; set; }
    [DataType(DataType.Password), Display(Name = "Confirm Password"), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")] public string ConfirmPassword { get; set; }
}

public class UserCreateVM : RegisterVM
{
    [Required(ErrorMessage = "Select a role.")] public string Role { get; set; }
}

public class UserEditVM
{
    public string Id { get; set; }
    [Required, StringLength(100), Display(Name = "Full Name")] public string FullName { get; set; }
    public string Email { get; set; }
    [Required(ErrorMessage = "Select a role.")] public string Role { get; set; }
    [Display(Name = "Active")] public bool IsActive { get; set; }
}

public class ResetPasswordVM
{
    public string Id { get; set; }
    [Required(ErrorMessage = "New password is required."), DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string NewPassword { get; set; }
}

public class UserRowVM
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.Now;
}

public class DashboardVM
{
    public string Range { get; set; } = "all";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int PendingFollowUps { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WeightedPipeline { get; set; }
    public List<string> LeadLabels { get; set; } = new();
    public List<int> LeadCounts { get; set; } = new();
    public List<string> OppLabels { get; set; } = new();
    public List<int> OppCounts { get; set; } = new();
    public List<string> MonthLabels { get; set; } = new();
    public List<decimal> MonthTotals { get; set; } = new();
    public List<FollowUp> Upcoming { get; set; } = new();
}
