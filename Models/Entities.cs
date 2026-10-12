using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

// ==================== APP USER ====================

public class AppUser : IdentityUser
{
    [StringLength(100)]
    public string FullName { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

// ==================== CUSTOMER ====================

public class Customer
{
    public int CustomerId { get; set; }

    [StringLength(20)]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Required]
    [RegularExpression(
        @"^[6-9]\d{9}$",
        ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";

    [StringLength(100)]
    public string? CompanyName { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(60)]
    public string? City { get; set; }

    [StringLength(60)]
    public string? State { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? AssignedTo { get; set; }
}

// ==================== LEAD ====================

public class Lead
{
    public int LeadId { get; set; }

    public string? LeadCode { get; set; }

    [Required]
    [StringLength(100)]
    public string LeadName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [RegularExpression(
        @"^[6-9]\d{9}$",
        ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";

    public string? CompanyName { get; set; }

    public string? Source { get; set; }

    [Required]
    [RegularExpression(
        "New|Contacted|Qualified|Unqualified|Converted|Lost",
        ErrorMessage = "Invalid lead status.")]
    public string Status { get; set; } = "New";

    [Range(
        0,
        100000000,
        ErrorMessage = "Expected value out of range.")]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }

    [StringLength(20)]
    public string Priority { get; set; } = "Medium";

    [StringLength(1000)]
    public string? Notes { get; set; }

    public AppUser? AssignedUser { get; set; }
}

// ==================== OPPORTUNITY ====================

public class Opportunity : IValidatableObject
{
    public int OpportunityId { get; set; }

    [Required]
    [StringLength(120)]
    public string OpportunityName { get; set; } = "";

    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [RegularExpression(
        "Qualification|Proposal|Negotiation|Won|Lost")]
    public string Stage { get; set; } = "Qualification";

    public int Probability { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today;

    public string Status { get; set; } = "Open";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Added property; declared only once.
    public DateTime? ClosedDate { get; set; }

    public string? AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        bool active = Stage != "Won" && Stage != "Lost";

        if (Amount < 0 || (active && Amount <= 0))
        {
            yield return new ValidationResult(
                "Opportunity Amount must be greater than 0.",
                new[] { nameof(Amount) });
        }

        if (Probability < 0 || Probability > 100)
        {
            yield return new ValidationResult(
                "Probability must be between 0 and 100.",
                new[] { nameof(Probability) });
        }

        if (active && ExpectedCloseDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Expected Close Date cannot be in the past.",
                new[] { nameof(ExpectedCloseDate) });
        }
    }
}

// ==================== FOLLOW-UP ====================

public class FollowUp : IValidatableObject
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    // Added properties.
    public string Subject { get; set; } = "";

    public Customer? Customer { get; set; }

    public Lead? Lead { get; set; }

    public DateTime FollowUpDate { get; set; }

    public string FollowUpType { get; set; } = "Call";

    public string? Remarks { get; set; }

    [RegularExpression("Planned|Completed|Missed|Cancelled")]
    public string Status { get; set; } = "Planned";

    public string? AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (Status == "Planned" &&
            FollowUpDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Follow-up date cannot be earlier than today.",
                new[] { nameof(FollowUpDate) });
        }
    }
}

// ==================== ACTIVITY ====================

public class Activity
{
    public int ActivityId { get; set; }

    [RegularExpression("Call|Meeting|Email|Task")]
    public string ActivityType { get; set; } = "Call";

    [Required]
    public string Subject { get; set; } = "";

    public string? Description { get; set; }

    public DateTime ActivityDate { get; set; } = DateTime.Today;

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    public string? AssignedTo { get; set; }

    public string Status { get; set; } = "Open";
}

// ==================== AUDIT LOG ====================

public class AuditLog
{
    public int AuditLogId { get; set; }

    public string? UserId { get; set; }

    public string Action { get; set; } = "";

    public string EntityName { get; set; } = "";

    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
}

// ==================== LOGIN VIEW MODEL ====================

public class LoginVm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

// ==================== REGISTER VIEW MODEL ====================

public class RegisterVm
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Compare(
        "Password",
        ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

// ==================== CUSTOMER DTO ====================

public record CustomerDto(
    int CustomerId,
    string CustomerName,
    string Email,
    string Phone,
    string? CompanyName,
    string Status);