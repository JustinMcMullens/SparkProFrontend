using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class AllocationEndpoints
{
    public static void MapAllocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/allocations")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetAllocations);
        group.MapPost("/{industry}/{id:int}/approve", ApproveAllocation);
        group.MapPost("/batch-approve", BatchApproveAllocations);

        var overridesGroup = app.MapGroup("/api/overrides")
            .RequireAuthorization("Authenticated");
        overridesGroup.MapGet("/", GetOverrides);
        overridesGroup.MapPost("/{id:int}/approve", ApproveOverride);

        var clawbacksGroup = app.MapGroup("/api/clawbacks")
            .RequireAuthorization("Authenticated");
        clawbacksGroup.MapGet("/", GetClawbacks);
    }

    private static async Task<IResult> GetAllocations(
        HttpContext http,
        AllocationQueryService allocations,
        PermissionsService permissions,
        int? page, int? pageSize,
        string? industry, int? userId, int? saleId,
        bool? isApproved, bool? isPaid)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var query = allocations.GetAllAllocations()
            .WhereIf(!string.IsNullOrEmpty(industry), a => a.Industry == industry)
            .WhereIf(userId.HasValue, a => a.UserId == userId!.Value)
            .WhereIf(saleId.HasValue, a => a.SaleId == saleId!.Value)
            .WhereIf(isApproved.HasValue, a => a.IsApproved == isApproved!.Value)
            .WhereIf(isPaid.HasValue, a => a.IsPaid == isPaid!.Value);

        // Authority scoping
        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(a => accessibleIds.Contains(a.UserId));
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> ApproveAllocation(
        string industry,
        int id,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;

        switch (industry.ToLower())
        {
            case "solar":
                var solar = await db.SolarCommissionAllocations.FindAsync(id);
                if (solar == null) return ApiResults.NotFound("Allocation not found");
                solar.IsApproved = true; solar.ApprovedAt = now; solar.ApprovedBy = currentUserId; solar.UpdatedAt = now;
                break;
            case "pest":
                var pest = await db.PestCommissionAllocations.FindAsync(id);
                if (pest == null) return ApiResults.NotFound("Allocation not found");
                pest.IsApproved = true; pest.ApprovedAt = now; pest.ApprovedBy = currentUserId; pest.UpdatedAt = now;
                break;
            case "roofing":
                var roofing = await db.RoofingCommissionAllocations.FindAsync(id);
                if (roofing == null) return ApiResults.NotFound("Allocation not found");
                roofing.IsApproved = true; roofing.ApprovedAt = now; roofing.ApprovedBy = currentUserId; roofing.UpdatedAt = now;
                break;
            case "fiber":
                var fiber = await db.FiberCommissionAllocations.FindAsync(id);
                if (fiber == null) return ApiResults.NotFound("Allocation not found");
                fiber.IsApproved = true; fiber.ApprovedAt = now; fiber.ApprovedBy = currentUserId; fiber.UpdatedAt = now;
                break;
            default:
                return ApiResults.BadRequest("Invalid industry");
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { AllocationId = id, Approved = true });
    }

    private static async Task<IResult> BatchApproveAllocations(
        BatchApproveRequest request,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;
        var approved = 0;
        var errors = new List<string>();

        foreach (var item in request.Allocations)
        {
            try
            {
                switch (item.Industry.ToLower())
                {
                    case "solar":
                        var solar = await db.SolarCommissionAllocations.FindAsync(item.AllocationId);
                        if (solar != null) { solar.IsApproved = true; solar.ApprovedAt = now; solar.ApprovedBy = currentUserId; solar.UpdatedAt = now; approved++; }
                        break;
                    case "pest":
                        var pest = await db.PestCommissionAllocations.FindAsync(item.AllocationId);
                        if (pest != null) { pest.IsApproved = true; pest.ApprovedAt = now; pest.ApprovedBy = currentUserId; pest.UpdatedAt = now; approved++; }
                        break;
                    case "roofing":
                        var roofing = await db.RoofingCommissionAllocations.FindAsync(item.AllocationId);
                        if (roofing != null) { roofing.IsApproved = true; roofing.ApprovedAt = now; roofing.ApprovedBy = currentUserId; roofing.UpdatedAt = now; approved++; }
                        break;
                    case "fiber":
                        var fiber = await db.FiberCommissionAllocations.FindAsync(item.AllocationId);
                        if (fiber != null) { fiber.IsApproved = true; fiber.ApprovedAt = now; fiber.ApprovedBy = currentUserId; fiber.UpdatedAt = now; approved++; }
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{item.Industry}/{item.AllocationId}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { Approved = approved, Total = request.Allocations.Count, Errors = errors });
    }

    private static async Task<IResult> GetOverrides(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        int? page, int? pageSize,
        int? userId, int? saleId, bool? isApproved, bool? isPaid)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var query = db.OverrideAllocations
            .WhereIf(userId.HasValue, o => o.UserId == userId!.Value)
            .WhereIf(saleId.HasValue, o => o.SaleId == saleId!.Value)
            .WhereIf(isApproved.HasValue, o => o.IsApproved == isApproved!.Value)
            .WhereIf(isPaid.HasValue, o => o.IsPaid == isPaid!.Value)
            .AsQueryable();

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(o => accessibleIds.Contains(o.UserId));
        }

        var projected = query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.AllocationId,
                o.SaleId,
                o.UserId,
                o.OverrideLevel,
                o.AllocatedAmount,
                o.IsApproved,
                o.ApprovedAt,
                o.IsPaid,
                o.PaidAt,
                o.PayrollBatchId,
                o.CreatedAt
            });

        var result = await projected.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> ApproveOverride(
        int id,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var oa = await db.OverrideAllocations.FindAsync(id);
        if (oa == null) return ApiResults.NotFound("Override allocation not found");

        oa.IsApproved = true;
        oa.ApprovedAt = DateTime.UtcNow;
        oa.ApprovedBy = currentUserId;
        oa.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { AllocationId = id, Approved = true });
    }

    private static async Task<IResult> GetClawbacks(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        int? page, int? pageSize,
        int? userId, int? saleId, bool? isProcessed)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var query = db.Clawbacks
            .WhereIf(userId.HasValue, c => c.UserId == userId!.Value)
            .WhereIf(saleId.HasValue, c => c.SaleId == saleId!.Value)
            .WhereIf(isProcessed.HasValue, c => c.IsProcessed == isProcessed!.Value)
            .AsQueryable();

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(c => accessibleIds.Contains(c.UserId));
        }

        var projected = query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.ClawbackId,
                c.SaleId,
                c.UserId,
                c.ClawbackAmount,
                c.ClawbackReason,
                c.ClawbackDate,
                c.IsProcessed,
                c.ProcessedAt,
                c.CreatedAt
            });

        var result = await projected.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }
}
