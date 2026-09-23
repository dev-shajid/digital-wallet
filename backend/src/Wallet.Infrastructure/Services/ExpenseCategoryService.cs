using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// CRUD for expense categories. Stays thin: validation, DB write, mapping to
/// <see cref="ExpenseCategoryResponse"/>. No balance math here - that lives in
/// ExpenseService (separate task).
/// </summary>
public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly WalletDbContext _dbContext;

    public ExpenseCategoryService(WalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ExpenseCategoryResponse>>> GetAllAsync(
        CancellationToken ct = default)
    {
        var rows = await _dbContext.ExpenseCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return Ok(rows.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<List<ExpenseCategoryResponse>>> GetActiveAsync(
        CancellationToken ct = default)
    {
        var rows = await _dbContext.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.Status == CategoryStatus.ACTIVE)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return Ok(rows.Select(ToResponse).ToList());
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> CreateAsync(
        ExpenseCategoryRequest request,
        CancellationToken ct = default)
    {
        var normalized = (request.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw DomainException.BadRequest(
                "Category name is required.",
                field: nameof(request.Name));
        }

        var taken = await _dbContext.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(
                c => c.Name.ToLower() == normalized.ToLower(),
                ct);

        if (taken)
        {
            throw DomainException.Conflict(
                $"An expense category named '{normalized}' already exists.",
                field: nameof(request.Name));
        }

        var entity = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Name = normalized,
            Description = request.Description?.Trim() ?? string.Empty,
            // New categories always start ACTIVE - clients cannot pick the status on create.
            Status = CategoryStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ExpenseCategories.Add(entity);
        await SaveWithUniqueNameGuardAsync(ct);

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = "Expense category created successfully.",
            Data = ToResponse(entity)
        };
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> UpdateAsync(
        Guid id,
        ExpenseCategoryRequest request,
        CancellationToken ct = default)
    {
        var entity = await _dbContext.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw DomainException.NotFound(
                $"No expense category with id {id}.");

        var normalized = (request.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw DomainException.BadRequest(
                "Category name is required.",
                field: nameof(request.Name));
        }

        var taken = await _dbContext.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(
                c => c.Id != id &&
                     c.Name.ToLower() == normalized.ToLower(),
                ct);

        if (taken)
        {
            throw DomainException.Conflict(
                $"An expense category named '{normalized}' already exists.",
                field: nameof(request.Name));
        }

        entity.Name = normalized;
        entity.Description = request.Description?.Trim() ?? string.Empty;
        // request.Status is non-null because the [Required] validator already
        // rejected null at the controller boundary; the fallback is just to
        // satisfy the nullable-type compiler.
        entity.Status = request.Status ?? CategoryStatus.ACTIVE;
        entity.UpdatedAt = DateTime.UtcNow;

        await SaveWithUniqueNameGuardAsync(ct);

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Expense category updated successfully.",
            Data = ToResponse(entity)
        };
    }

    public async Task<ApiResponse<ExpenseCategoryResponse>> SetStatusAsync(
        Guid id,
        CategoryStatus status,
        CancellationToken ct = default)
    {
        var entity = await _dbContext.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw DomainException.NotFound(
                $"No expense category with id {id}.");

        if (entity.Status != status)
        {
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
        }

        var verb = status == CategoryStatus.INACTIVE
            ? "deactivated"
            : "activated";

        return new ApiResponse<ExpenseCategoryResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = $"Expense category {verb} successfully.",
            Data = ToResponse(entity)
        };
    }

    public async Task<ApiResponse<object?>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw DomainException.NotFound($"No expense category with id {id}.");

        // Pre-check the FK count so we return a friendly 409 instead of the raw
        // Postgres 23503 (foreign_key_violation) from SaveChanges. The DB constraint
        // is still the source of truth and would catch a race against a concurrent
        // insert, but it can't say how many expenses blocked us.
        var inUse = await _dbContext.Expenses
            .AsNoTracking()
            .CountAsync(e => e.CategoryId == id, ct);
        if (inUse > 0)
        {
            throw DomainException.Conflict(
                $"Cannot delete category '{entity.Name}' because it is referenced by {inUse} expense(s). " +
                "Set Status to INACTIVE instead to hide it from new expenses while keeping history.");
        }

        _dbContext.ExpenseCategories.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<object?>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = $"Expense category '{entity.Name}' deleted successfully.",
            Data = new { id = entity.Id, name = entity.Name }
        };
    }

    /// <summary>
    /// Saves and turns the EF unique-violation (Postgres SQLSTATE 23505)
    /// into a 409 DomainException.
    /// </summary>
    private async Task SaveWithUniqueNameGuardAsync(CancellationToken ct)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState == "23505")
        {
            throw DomainException.Conflict(
                "An expense category with that name already exists.",
                field: "name");
        }
    }

    private static ApiResponse<List<ExpenseCategoryResponse>> Ok(
        List<ExpenseCategoryResponse> data)
        => new()
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Request completed successfully.",
            Data = data
        };

    private static ExpenseCategoryResponse ToResponse(
        ExpenseCategory c)
        => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
}