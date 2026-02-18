using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Middleware;
using System.Security.Claims;

namespace SparkBackend.Endpoints;

public static class RoofingCalculatorEndpoints
{
    public static void MapRoofingCalculatorEndpoints(this WebApplication app)
    {
        // ---------- GET: Preview Commission Calculation ----------
        app.MapGet("/api/sales/{saleId}/roofing-commission-calculator", async (
            int saleId,
            HttpRequest req,
            SparkDbContext db,
            HttpContext httpContext
        ) =>
        {
            try
            {
                // Get milestone from query params (default to preview both)
                var milestoneParam = req.Query["milestone"].ToString();
                int? milestone = string.IsNullOrEmpty(milestoneParam) ? null : int.Parse(milestoneParam);

                // Load sale with all related data
                var sale = await db.Sales
                    .Include(s => s.ProjectType)
                    .Include(s => s.Customer)
                    .Include(s => s.RoofingSale)
                    .Include(s => s.SaleParticipants)
                        .ThenInclude(sp => sp.User)
                            .ThenInclude(u => u.User)
                    .Include(s => s.SaleParticipants)
                        .ThenInclude(sp => sp.Role)
                    .FirstOrDefaultAsync(s => s.SaleId == saleId);

                if (sale == null)
                    return Results.NotFound(new { error = $"Sale {saleId} not found" });

                if (!string.Equals(sale.ProjectType?.ProjectTypeName, "roof", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "This endpoint is for roofing projects only" });

                if (!sale.ContractAmount.HasValue || sale.ContractAmount.Value == 0)
                    return Results.BadRequest(new { error = "Contract amount is required for commission calculation" });

                // Calculate commissions
                var result = new
                {
                    saleId = sale.SaleId,
                    contractAmount = sale.ContractAmount,
                    saleDate = sale.SaleDate,
                    saleStatus = sale.SaleStatus,
                    customer = new
                    {
                        name = $"{sale.Customer?.FirstName} {sale.Customer?.LastName}".Trim(),
                        address = sale.Customer?.AddressLine1
                    },
                    roofingDetails = new
                    {
                        frontendReceivedAmount = sale.RoofingSale?.FrontendReceivedAmount,
                        backendReceivedAmount = sale.RoofingSale?.BackendReceivedAmount,
                        projectValue = sale.RoofingSale?.ProjectValue
                    },
                    milestones = new
                    {
                        mp1 = milestone == null || milestone == 1 ? await CalculateMilestoneAsync(db, sale, 1) : null,
                        mp2 = milestone == null || milestone == 2 ? await CalculateMilestoneAsync(db, sale, 2) : null
                    }
                };

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error calculating roofing commission: {ex.Message}");
            }
        })
        .RequireAuthorization("Authenticated")
        .WithName("GetRoofingCommissionPreview")
        .WithTags("Commission Calculator");

        // ---------- POST: Save Commission Calculation ----------
        app.MapPost("/api/sales/{saleId}/roofing-commission-calculator", async (
            int saleId,
            CommissionSaveRequest request,
            SparkDbContext db,
            HttpContext httpContext
        ) =>
        {
            try
            {
                // Get authenticated user ID
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                    return Results.Unauthorized();

                // Validate request
                if (request.Milestone < 1 || request.Milestone > 2)
                    return Results.BadRequest(new { error = "Milestone must be 1 (MP1) or 2 (MP2)" });

                // Load sale
                var sale = await db.Sales
                    .Include(s => s.ProjectType)
                    .Include(s => s.RoofingSale)
                    .Include(s => s.SaleParticipants)
                        .ThenInclude(sp => sp.Role)
                    .FirstOrDefaultAsync(s => s.SaleId == saleId);

                if (sale == null)
                    return Results.NotFound(new { error = $"Sale {saleId} not found" });

                if (!string.Equals(sale.ProjectType?.ProjectTypeName, "roof", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "This endpoint is for roofing projects only" });

                // Calculate and save allocations
                var savedAllocations = await SaveMilestoneAllocationsAsync(
                    db,
                    sale,
                    request.Milestone,
                    currentUserId
                );

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = "Commission calculation saved successfully",
                    saleId = sale.SaleId,
                    milestone = request.Milestone,
                    allocationsCreated = savedAllocations.Count
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error saving roofing commission: {ex.Message}");
            }
        })
        .RequireAuthorization("Level4Plus")
        .WithName("SaveRoofingCommissionCalculation")
        .WithTags("Commission Calculator");
    }

    // ---------- Helper Methods ----------

    private static async Task<object> CalculateMilestoneAsync(SparkDbContext db, Sale sale, int milestoneNumber)
    {
        var allocations = new List<object>();
        var overrides = new List<object>();
        decimal totalAllocated = 0m;

        // Get commissionable amount for this milestone
        decimal commissionableAmount = GetCommissionableAmount(sale, milestoneNumber);

        // Calculate allocations for each participant
        foreach (var participant in sale.SaleParticipants)
        {
            var rate = await FindBestMatchingRateAsync(
                db,
                participant.UserId,
                participant.RoleId,
                null, // Roofing doesn't track installer at sale level
                sale.Customer?.StateCode,
                sale.SaleDate
            );

            if (rate != null)
            {
                decimal amount = CalculateAllocationAmount(
                    commissionableAmount,
                    milestoneNumber,
                    rate
                );

                allocations.Add(new
                {
                    userId = participant.UserId,
                    userName = $"{participant.User?.User?.FirstName} {participant.User?.User?.LastName}".Trim(),
                    roleName = participant.Role?.RoleName,
                    percentRate = milestoneNumber == 1 ? rate.PercentMp1 : rate.PercentMp2,
                    flatRate = milestoneNumber == 1 ? rate.FlatMp1 : rate.FlatMp2,
                    allocatedAmount = amount,
                    rateId = rate.RateId
                });

                totalAllocated += amount;

                // Calculate manager overrides for this participant
                var participantOverrides = await CalculateOverridesAsync(
                    db,
                    participant.UserId,
                    commissionableAmount,
                    milestoneNumber,
                    sale.SaleDate
                );
                overrides.AddRange(participantOverrides);
            }
            else
            {
                // No rate found - log warning
                allocations.Add(new
                {
                    userId = participant.UserId,
                    userName = $"{participant.User?.User?.FirstName} {participant.User?.User?.LastName}".Trim(),
                    roleName = participant.Role?.RoleName,
                    warning = "No commission rate found for this user/role combination",
                    allocatedAmount = 0m
                });
            }
        }

        decimal totalOverrides = overrides.Cast<dynamic>().Sum(o => (decimal)o.amount);

        return new
        {
            milestoneNumber,
            milestoneName = milestoneNumber == 1 ? "MP1 (Approved)" : "MP2 (Completed)",
            commissionableAmount,
            participants = allocations,
            overrides,
            totals = new
            {
                participantAllocations = totalAllocated,
                managerOverrides = totalOverrides,
                grandTotal = totalAllocated + totalOverrides
            }
        };
    }

    private static decimal GetCommissionableAmount(Sale sale, int milestoneNumber)
    {
        // For roofing, MP1 uses frontend amount, MP2 uses backend amount
        // Fall back to contract amount if specific amounts not set
        if (milestoneNumber == 1)
        {
            return sale.RoofingSale?.FrontendReceivedAmount
                ?? (sale.ContractAmount ?? 0m) * 0.5m; // Default to 50% if not specified
        }
        else // MP2
        {
            return sale.RoofingSale?.BackendReceivedAmount
                ?? (sale.ContractAmount ?? 0m) * 0.5m; // Default to 50% if not specified
        }
    }

    private static async Task<RoofingUserCommissionRate?> FindBestMatchingRateAsync(
        SparkDbContext db,
        int userId,
        int? roleId,
        int? installerId,
        string? stateCode,
        DateOnly saleDate
    )
    {
        // Rate lookup with specificity hierarchy
        // Most specific match wins (more non-null criteria = higher priority)
        var rates = await db.RoofingUserCommissionRates
            .Where(r => r.UserId == userId)
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveStartDate <= saleDate)
            .Where(r => r.EffectiveEndDate == null || r.EffectiveEndDate >= saleDate)
            .Where(r => r.RoleId == null || r.RoleId == roleId)
            .Where(r => r.InstallerId == null || r.InstallerId == installerId)
            .Where(r => r.StateCode == null || r.StateCode == stateCode)
            .ToListAsync();

        // Order by specificity (count of non-null matching criteria)
        return rates
            .OrderByDescending(r =>
                (r.RoleId.HasValue && r.RoleId == roleId ? 1 : 0) +
                (r.InstallerId.HasValue && r.InstallerId == installerId ? 1 : 0) +
                (!string.IsNullOrEmpty(r.StateCode) && r.StateCode == stateCode ? 1 : 0)
            )
            .FirstOrDefault();
    }

    private static decimal CalculateAllocationAmount(
        decimal commissionableAmount,
        int milestoneNumber,
        RoofingUserCommissionRate rate
    )
    {
        // Get rates for this milestone
        decimal? percentRate = milestoneNumber == 1 ? rate.PercentMp1 : rate.PercentMp2;
        decimal? flatRate = milestoneNumber == 1 ? rate.FlatMp1 : rate.FlatMp2;

        // Calculate: (amount × percent / 100) + flat
        decimal percentAmount = percentRate.HasValue
            ? commissionableAmount * percentRate.Value / 100m
            : 0m;
        decimal flatAmount = flatRate ?? 0m;

        return Math.Round(percentAmount + flatAmount, 2);
    }

    private static async Task<List<object>> CalculateOverridesAsync(
        SparkDbContext db,
        int salesRepUserId,
        decimal commissionableAmount,
        int milestoneNumber,
        DateOnly saleDate
    )
    {
        var overrides = new List<object>();
        var currentUserId = salesRepUserId;
        var level = 1;
        const int MaxOverrideLevels = 5;

        while (level <= MaxOverrideLevels)
        {
            // Get employee record to find manager
            var employee = await db.Employees
                .Include(e => e.Manager)
                    .ThenInclude(m => m!.User)
                .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

            if (employee?.ManagerId == null)
                break; // No more managers in chain

            var manager = employee.Manager;
            if (manager == null || !manager.IsActive)
                break; // Manager not active

            // Avoid infinite loops if manager points to self
            if (manager.UserId == salesRepUserId)
                break;

            // Find override rate for this manager
            var overrideRate = await FindBestMatchingRateAsync(
                db,
                manager.UserId,
                null, // Overrides typically not role-specific
                null,
                null,
                saleDate
            );

            if (overrideRate != null)
            {
                decimal overrideAmount = CalculateAllocationAmount(
                    commissionableAmount,
                    milestoneNumber,
                    overrideRate
                );

                if (overrideAmount > 0)
                {
                    overrides.Add(new
                    {
                        userId = manager.UserId,
                        userName = $"{manager.User?.FirstName} {manager.User?.LastName}".Trim(),
                        overrideLevel = level,
                        amount = overrideAmount,
                        rateId = overrideRate.RateId
                    });
                }
            }

            // Move up the chain
            currentUserId = manager.UserId;
            level++;
        }

        return overrides;
    }

    private static async Task<List<RoofingCommissionAllocation>> SaveMilestoneAllocationsAsync(
        SparkDbContext db,
        Sale sale,
        int milestoneNumber,
        int currentUserId
    )
    {
        var savedAllocations = new List<RoofingCommissionAllocation>();
        decimal commissionableAmount = GetCommissionableAmount(sale, milestoneNumber);

        // Get or create allocation type
        var allocationType = await db.AllocationTypes
            .FirstOrDefaultAsync(at =>
                at.AllocationTypeName == "Closer" ||
                at.AllocationTypeName == "Participant"
            );

        if (allocationType == null)
        {
            // Create default allocation type if missing
            allocationType = new AllocationType
            {
                AllocationTypeName = "Participant",
                Description = "Sale participant commission",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.AllocationTypes.Add(allocationType);
            await db.SaveChangesAsync();
        }

        // Save allocations for each participant
        foreach (var participant in sale.SaleParticipants)
        {
            var rate = await FindBestMatchingRateAsync(
                db,
                participant.UserId,
                participant.RoleId,
                null, // Roofing doesn't track installer at sale level
                sale.Customer?.StateCode,
                sale.SaleDate
            );

            if (rate == null)
                continue; // Skip if no rate found

            decimal amount = CalculateAllocationAmount(commissionableAmount, milestoneNumber, rate);

            if (amount <= 0)
                continue; // Skip zero allocations

            // Check if allocation already exists
            var existing = await db.RoofingCommissionAllocations
                .FirstOrDefaultAsync(a =>
                    a.SaleId == sale.SaleId &&
                    a.UserId == participant.UserId &&
                    a.MilestoneNumber == milestoneNumber
                );

            if (existing == null)
            {
                var allocation = new RoofingCommissionAllocation
                {
                    SaleId = sale.SaleId,
                    UserId = participant.UserId,
                    AllocationTypeId = allocationType.AllocationTypeId,
                    MilestoneNumber = milestoneNumber,
                    AllocatedAmount = amount,
                    IsApproved = false,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.RoofingCommissionAllocations.Add(allocation);
                savedAllocations.Add(allocation);
            }
            else
            {
                // Update existing allocation
                existing.AllocatedAmount = amount;
                existing.UpdatedAt = DateTime.UtcNow;
                savedAllocations.Add(existing);
            }
        }

        // Save override allocations separately
        foreach (var participant in sale.SaleParticipants)
        {
            await SaveOverrideAllocationsAsync(
                db,
                sale,
                participant.UserId,
                commissionableAmount,
                milestoneNumber,
                currentUserId
            );
        }

        return savedAllocations;
    }

    private static async Task SaveOverrideAllocationsAsync(
        SparkDbContext db,
        Sale sale,
        int salesRepUserId,
        decimal commissionableAmount,
        int milestoneNumber,
        int currentUserId
    )
    {
        var currentEmpUserId = salesRepUserId;
        var level = 1;
        const int MaxOverrideLevels = 5;

        while (level <= MaxOverrideLevels)
        {
            var employee = await db.Employees
                .FirstOrDefaultAsync(e => e.UserId == currentEmpUserId && e.IsActive);

            if (employee?.ManagerId == null)
                break;

            var manager = await db.Employees
                .FirstOrDefaultAsync(e => e.UserId == employee.ManagerId.Value && e.IsActive);

            if (manager == null)
                break;

            var overrideRate = await FindBestMatchingRateAsync(
                db,
                manager.UserId,
                null,
                null,
                null,
                sale.SaleDate
            );

            if (overrideRate != null)
            {
                decimal overrideAmount = CalculateAllocationAmount(
                    commissionableAmount,
                    milestoneNumber,
                    overrideRate
                );

                if (overrideAmount > 0)
                {
                    // Check if override already exists
                    var existing = await db.OverrideAllocations
                        .FirstOrDefaultAsync(o =>
                            o.SaleId == sale.SaleId &&
                            o.UserId == manager.UserId &&
                            o.OverrideLevel == level
                        );

                    if (existing == null)
                    {
                        var overrideAllocation = new OverrideAllocation
                        {
                            SaleId = sale.SaleId,
                            UserId = manager.UserId,
                            OverrideLevel = level,
                            AllocatedAmount = overrideAmount,
                            IsApproved = false,
                            IsPaid = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        db.OverrideAllocations.Add(overrideAllocation);
                    }
                    else
                    {
                        existing.AllocatedAmount = overrideAmount;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            currentEmpUserId = manager.UserId;
            level++;
        }
    }
}

// ---------- Request/Response Models ----------

public record CommissionSaveRequest(
    int Milestone
);
