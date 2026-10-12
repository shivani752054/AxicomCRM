using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

/// <summary>Server-side + client-side (unobtrusive) rule: date cannot be earlier than today.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotBeforeTodayAttribute : ValidationAttribute, IClientModelValidator
{
    public NotBeforeTodayAttribute() { ErrorMessage = "Date cannot be earlier than today."; }

    protected override ValidationResult IsValid(object value, ValidationContext ctx)
        => value is DateTime d && d.Date < DateTime.Today ? new ValidationResult(ErrorMessage) : ValidationResult.Success;

    public void AddValidation(ClientModelValidationContext context)
    {
        Merge(context.Attributes, "data-val", "true");
        Merge(context.Attributes, "data-val-notbeforetoday", ErrorMessage);
    }
    static void Merge(IDictionary<string, string> a, string k, string v) { if (!a.ContainsKey(k)) a.Add(k, v); }
}
