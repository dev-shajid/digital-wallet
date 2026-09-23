using Microsoft.EntityFrameworkCore;
using Wallet.Application.Common.Models;
using Wallet.Application.Expenses;
using Wallet.Application.Expenses.Models;
using Wallet.Domain.Entities;
using Wallet.Domain.Enums;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Services;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly AppDbContext _dbContext;

    public ExpenseCategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<ApiResponse<List<ExpenseCategoryResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _dbContext.ExpenseCategories
            .OrderBy(c => c.Name)
            .Select(c => MapToResponse(c))
            .ToListAsync(ct);

        return new ApiResponse<List<ExpenseCategoryResponse>>
        {
            Success = true,
            Status = 200,
            Message = "Categories retrieved successfully.",
            Data = categories
        };
    }

    public async Task<ApiResponse<List<ExpenseCategoryResponse>>> GetActiveAsync(CancellationToken ct = default)
    {
        var categories = await _dbContext.ExpenseCategories
            .Where(c => c.Status == CategoryStatus.ACTIVE)
            .OrderBy(c => c.Name)
            .Select(c => MapToResponse(c))
            .ToListAsync(ct);

        return new ApiResponse<List<ExpenseCategoryResponse>>
        {
            Success = true,
            Status = 200,
            Message = "Active categories retrieved successfully.",
            Data = categories
        };
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> CreateAsync(ExpenseCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Fail(400, "Category name is required.");
        }

        var exists = await _dbContext.ExpenseCategories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);

        if (exists)
        {
            return Fail(409, "A category with this name already exists.");
        }

        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description ?? string.Empty,
            Status = CategoryStatus.ACTIVE
        };

        _dbContext.ExpenseCategories.Add(category);
        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = 201,
            Message = "Category created successfully.",
            Data = MapToResponse(category)
        };
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> UpdateAsync(Guid id, ExpenseCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _dbContext.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return Fail(404, "Category not found.");
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return Fail(400, "Category name is required.");
        }

        var nameTaken = await _dbContext.ExpenseCategories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct);

        if (nameTaken)
        {
            return Fail(409, "A category with this name already exists.");
        }

        category.Name = name;
        category.Description = request.Description ?? string.Empty;

        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = 200,
            Message = "Category updated successfully.",
            Data = MapToResponse(category)
        };
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> SetStatusAsync(Guid id, CategoryStatus status, CancellationToken ct = default)
    {
        var category = await _dbContext.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return Fail(404, "Category not found.");
        }

        category.Status = status;
        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = 200,
            Message = $"Category status updated to {status}.",
            Data = MapToResponse(category)
        };
    }

    private static ExpenseCategoryResponse MapToResponse(ExpenseCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Status = c.Status.ToString()
    };

    private static ApiResponse<ExpenseCategoryResponse> Fail(int status, string message) => new()
    {
        Success = false,
        Status = status,
        Message = message
    };
}