using System.ComponentModel.DataAnnotations;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Application.ExpenseCategories.Models;

/// <summary>
/// Body for PUT <c>/admin/expense-categories/{id}</c>. Updates name, description,
/// and status in a single call so renaming a category and (de)activating it is
/// one round trip instead of two. Status is required: callers must explicitly
/// say which state they want the row in.
/// </summary>
public class ExpenseCategoryUpdateRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Status string from the two allowed values. Nullable so a missing field
    /// fails the <see cref="RequiredAttribute"/> check instead of silently
    /// defaulting to <c>CategoryStatus.ACTIVE</c>.
    /// </summary>
    [Required(ErrorMessage = "Status is required.")]
    [EnumDataType(typeof(CategoryStatus), ErrorMessage = "Status must be either ACTIVE or INACTIVE.")]
    public CategoryStatus? Status { get; set; }
}
