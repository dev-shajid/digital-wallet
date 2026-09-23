using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.ExpenseCategories.Models;

/// <summary>Body for POST/PUT on <c>/admin/expense-categories</c>. Status is never editable here - use the dedicated PATCH endpoint.</summary>
public class ExpenseCategoryRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;
}
