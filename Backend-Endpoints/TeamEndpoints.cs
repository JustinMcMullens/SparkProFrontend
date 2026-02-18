using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/team")
            .RequireAuthorization("Authenticated");

        group.MapGet("/sales", GetTeamSales);
        group.MapGet("/members", GetTeamMembers);
        group.MapGet("/performance", GetTeamPerformance);
        group.MapGet("/pending-approvals", GetPendingApprovals);
    }

    private static async Task<IResult> GetTeamSales(
        HttpContext http,
        SparkDbContext db,
        TeamHierarchyService teamService,
        int? page, int? pageSize,
        string? status, DateOnly? dateFrom, DateOnly? dateTo,
        int? projectTypeId, int? userId,
        string? sortBy, string? sortDir)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out var currentUserId);
        if (authResult != null) return authResult;

        var managedIds = await teamService.GetManagedUserIdsAsync(currentUserId);
        managedIds.Add(currentUserId); // include self

        var query = db.Sales
            .Where(s => s.SaleParticipants.Any(sp => managedIds.Contains(sp.UserId)))
            .WhereIf(!string.IsNullOrEmpty(status), s => s.SaleStatus == status)
            .WhereIf(dateFrom.HasValue, s => s.SaleDate >= dateFrom!.Value)
            .WhereIf(dateTo.HasValue, s => s.SaleDate <= dateTo!.Value)
            .WhereIf(projectTypeId.HasValue, s => s.ProjectTypeId == projectTypeId!.Value)
            .WhereIf(userId.HasValue, s => s.SaleParticipants.Any(sp => sp.UserId == userId!.Value));

        query = (sortBy?.ToLower()) switch
        {
            "date" => sortDir == "asc" ? query.OrderBy(s => s.SaleDate) : query.OrderByDescending(s => s.SaleDate),
            "amount" => sortDir == "asc" ? query.OrderBy(s => s.ContractAmount) : query.OrderByDescending(s => s.ContractAmount),
            _ => query.OrderByDescending(s => s.SaleDate)
        };

        var projected = query.Select(s => new
        {
            s.SaleId,
            s.SaleDate,
            s.SaleStatus,
            s.ContractAmount,
            ProjectType = s.ProjectType.ProjectTypeName,
            CustomerName = s.Customer.FirstName + " " + s.Customer.LastName,
            Participants = s.SaleParticipants.Select(sp => new
            {
                sp.UserId,
                sp.User.User.FirstName,
                sp.User.User.LastName,
                Role = sp.Role.RoleName,
                sp.SplitPercent
            }),
            s.CreatedAt
        });

        var result = await projected.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetTeamMembers(
        HttpContext http,
        SparkDbContext db,
        TeamHierarchyService teamService,
        AllocationQueryService allocations)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out var currentUserId);
        if (authResult != null) return authResult;

        var managedIds = await teamService.GetManagedUserIdsAsync(currentUserId, includeIndirect: false);

        var members = await db.Employees
            .Where(e => managedIds.Contains(e.UserId) && e.IsActive)
            .Include(e => e.User)
            .Include(e => e.Title)
            .Include(e => e.Team)
            .Select(e => new
            {
                e.UserId,
                e.User.FirstName,
                e.User.LastName,
                e.User.Email,
                e.User.Phone,
                Title = e.Title != null ? e.Title.TitleName : null,
                Team = e.Team != null ? e.Team.TeamName : null,
                e.HireDate,
                e.IsActive
            })
            .ToListAsync();

        // Enrich with commission stats for current month
        var startDt = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToDateTime(TimeOnly.MinValue);
        var endDt = DateTime.UtcNow;

        var memberStats = new List<object>();
        foreach (var member in members)
        {
            var totalEarnings = await allocations.GetAllocationsForUser(member.UserId)
                .Where(a => a.CreatedAt >= startDt && a.CreatedAt <= endDt)
                .SumAsync(a => a.AllocatedAmount);

            var saleCount = await db.Sales
                .Where(s => s.SaleParticipants.Any(sp => sp.UserId == member.UserId)
                    && s.SaleDate >= DateOnly.FromDateTime(startDt)
                    && s.SaleDate <= DateOnly.FromDateTime(endDt))
                .CountAsync();

            memberStats.Add(new
            {
                member.UserId,
                member.FirstName,
                member.LastName,
                member.Email,
                member.Phone,
                member.Title,
                member.Team,
                member.HireDate,
                member.IsActive,
                Stats = new { MonthlyEarnings = totalEarnings, MonthlySales = saleCount }
            });
        }

        return ApiResults.Success(memberStats);
    }

    private static async Task<IResult> GetTeamPerformance(
        HttpContext http,
        SparkDbContext db,
        TeamHierarchyService teamService,
        AllocationQueryService allocations,
        DateOnly? periodStart, DateOnly? periodEnd)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out var currentUserId);
        if (authResult != null) return authResult;

        var managedIds = await teamService.GetManagedUserIdsAsync(currentUserId);
        managedIds.Add(currentUserId);

        var start = periodStart ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = periodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startDt = start.ToDateTime(TimeOnly.MinValue);
        var endDt = end.ToDateTime(TimeOnly.MaxValue);

        var totalSales = await db.Sales
            .Where(s => s.SaleParticipants.Any(sp => managedIds.Contains(sp.UserId))
                && s.SaleDate >= start && s.SaleDate <= end)
            .CountAsync();

        var totalValue = await db.Sales
            .Where(s => s.SaleParticipants.Any(sp => managedIds.Contains(sp.UserId))
                && s.SaleDate >= start && s.SaleDate <= end)
            .SumAsync(s => s.ContractAmount ?? 0);

        var totalCommissions = await allocations.GetAllAllocations()
            .Where(a => managedIds.Contains(a.UserId) && a.CreatedAt >= startDt && a.CreatedAt <= endDt)
            .SumAsync(a => a.AllocatedAmount);

        var byMember = await allocations.GetAllAllocations()
            .Where(a => managedIds.Contains(a.UserId) && a.CreatedAt >= startDt && a.CreatedAt <= endDt)
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalEarnings = g.Sum(a => a.AllocatedAmount),
                AllocationCount = g.Count()
            })
            .OrderByDescending(x => x.TotalEarnings)
            .ToListAsync();

        // Enrich with names
        var userIds = byMember.Select(b => b.UserId).ToList();
        var users = await db.Employees
            .Where(e => userIds.Contains(e.UserId))
            .Include(e => e.User)
            .ToDictionaryAsync(e => e.UserId);

        var byMemberEnriched = byMember.Select(b => new
        {
            b.UserId,
            FirstName = users.TryGetValue(b.UserId, out var emp) ? emp.User.FirstName : null,
            LastName = users.TryGetValue(b.UserId, out var emp2) ? emp2.User.LastName : null,
            b.TotalEarnings,
            b.AllocationCount
        });

        return ApiResults.Success(new
        {
            Period = new { Start = start, End = end },
            TeamSize = managedIds.Count,
            TotalSales = totalSales,
            TotalValue = totalValue,
            TotalCommissions = totalCommissions,
            ByMember = byMemberEnriched
        });
    }

    private static async Task<IResult> GetPendingApprovals(
        HttpContext http,
        AllocationQueryService allocations,
        int? page, int? pageSize)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out _);
        if (authResult != null) return authResult;

        var query = allocations.GetAllAllocations()
            .Where(a => !a.IsApproved)
            .OrderByDescending(a => a.CreatedAt);

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }
}
