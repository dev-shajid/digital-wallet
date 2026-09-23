using System.ComponentModel.DataAnnotations;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Application.ExpenseCategories.Models;

/// <summary>Body for PATCH <c>/admin/expense-categories/{id}/status</c>. Only "ACTIVE" and "INACTIVE" are allowed.</summary>
public class SetExpenseCategoryStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [EnumDataType(typeof(CategoryStatus), ErrorMessage = "Status must be either ACTIVE or INACTIVE.")]
    public CategoryStatus Status { get; set; }
}
