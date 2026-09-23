using System.ComponentModel.DataAnnotations;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Application.ExpenseCategories.Models;

/// <summary>
/// Body for PUT <c>/admin/expense-categories/{id}</c>. Every field is optional -
/// only the ones actually sent are changed - but at least one of them must be
/// present, otherwise there's nothing to update.
/// </summary>
public class ExpenseCategoryUpdateRequest : IValidatableObject
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [EnumDataType(typeof(CategoryStatus), ErrorMessage = "Status must be either ACTIVE or INACTIVE.")]
    public CategoryStatus? Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null && Description is null && Status is null)
        {
            yield return new ValidationResult(
                "Provide at least one of Name, Description, or Status to update.",
                [nameof(Name), nameof(Description), nameof(Status)]);
        }
    }
}
