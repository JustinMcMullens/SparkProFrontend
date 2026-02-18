using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization("Authenticated");

        group.MapGet("/stats", GetStats);
        group.MapGet("/recent-activity", GetRecentActivity);
        group.MapGet("/leaderboard", GetLeaderboard);
        group.MapGet("/announcements", GetAnnouncements);
    }

    private static async Task<IResult> GetStats(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        AllocationQueryService allocations,
        DateOnly? periodStart, DateOnly? periodEnd)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var start = periodStart ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = periodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Scope sales query by authority
        var salesQuery = db.Sales.Where(s => s.SaleDate >= start && s.SaleDate <= end);

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            salesQuery = salesQuery.Where(s => s.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)));
        }

        // Sales stats by status
        var salesByStatus = await salesQuery
            .GroupBy(s => s.SaleStatus)
            .Select(g => new { Status = g.Key, Count = g.Count(), TotalValue = g.Sum(s => s.ContractAmount ?? 0) })
            .ToListAsync();

        var totalSales = salesByStatus.Sum(s => s.Count);
        var totalValue = salesByStatus.Sum(s => s.TotalValue);

        // Allocation stats scoped to same users
        var allAllocations = allocations.GetAllAllocations()
            .Where(a => a.CreatedAt >= start.ToDateTime(TimeOnly.MinValue) && a.CreatedAt <= end.ToDateTime(TimeOnly.MaxValue));

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            allAllocations = allAllocations.Where(a => accessibleIds.Contains(a.UserId));
        }

        var pendingCommissions = await allAllocations.Where(a => !a.IsApproved).SumAsync(a => a.AllocatedAmount);
        var approvedCommissions = await allAllocations.Where(a => a.IsApproved && !a.IsPaid).SumAsync(a => a.AllocatedAmount);
        var paidCommissions = await allAllocations.Where(a => a.IsPaid).SumAsync(a => a.AllocatedAmount);

        // Approval queue count (Level 4+ only)
        int? approvalQueueCount = null;
        if (authorityLevel >= 4)
        {
            approvalQueueCount = await allocations.GetAllAllocations().CountAsync(a => !a.IsApproved);
        }

        return ApiResults.Success(new
        {
            Period = new { Start = start, End = end },
            Sales = new
            {
                Total = totalSales,
                TotalValue = totalValue,
                ByStatus = salesByStatus
            },
            Commissions = new
            {
                Pending = pendingCommissions,
                Approved = approvedCommissions,
                Paid = paidCommissions,
                Total = pendingCommissions + approvedCommissions + paidCommissions
            },
            ApprovalQueueCount = approvalQueueCount
        });
    }

    private static async Task<IResult> GetRecentActivity(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        int? count)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);
        var limit = Math.Clamp(count ?? 20, 1, 50);

        var query = db.Sales
            .Include(s => s.Customer)
            .Include(s => s.ProjectType)
            .AsQueryable();

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(s => s.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)));
        }

        var recentSales = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Take(limit)
            .Select(s => new
            {
                s.SaleId,
                s.SaleDate,
                s.SaleStatus,
                s.ContractAmount,
                ProjectType = s.ProjectType.ProjectTypeName,
                CustomerName = s.Customer.FirstName + " " + s.Customer.LastName,
                s.UpdatedAt,
                s.CreatedAt
            })
            .ToListAsync();

        return ApiResults.Success(recentSales);
    }

    private static async Task<IResult> GetLeaderboard(
        HttpContext http,
        SparkDbContext db,
        AllocationQueryService allocations,
        DateOnly? periodStart, DateOnly? periodEnd,
        int? limit)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out _);
        if (authResult != null) return authResult;

        var start = periodStart ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = periodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var top = Math.Clamp(limit ?? 10, 1, 50);

        var startDt = start.ToDateTime(TimeOnly.MinValue);
        var endDt = end.ToDateTime(TimeOnly.MaxValue);

        var leaders = await allocations.GetAllAllocations()
            .Where(a => a.CreatedAt >= startDt && a.CreatedAt <= endDt)
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalEarnings = g.Sum(a => a.AllocatedAmount),
                AllocationCount = g.Count()
            })
            .OrderByDescending(x => x.TotalEarnings)
            .Take(top)
            .ToListAsync();

        // Enrich with user names
        var userIds = leaders.Select(l => l.UserId).ToList();
        var users = await db.Employees
            .Where(e => userIds.Contains(e.UserId))
            .Include(e => e.User)
            .ToDictionaryAsync(e => e.UserId);

        var result = leaders.Select((l, index) => new
        {
            Rank = index + 1,
            l.UserId,
            FirstName = users.TryGetValue(l.UserId, out var emp) ? emp.User.FirstName : null,
            LastName = users.TryGetValue(l.UserId, out var emp2) ? emp2.User.LastName : null,
            l.TotalEarnings,
            l.AllocationCount
        });

        return ApiResults.Success(new { Period = new { Start = start, End = end }, Leaders = result });
    }

    private static async Task<IResult> GetAnnouncements(
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;

        // Get user's team/office/region for targeting
        var employee = await db.Employees
            .Include(e => e.Team)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

        var teamId = employee?.TeamId;
        var officeId = employee?.Team?.OfficeId;

        var announcements = await db.Announcements
            .Include(a => a.AnnouncementTargets)
            .Include(a => a.Author).ThenInclude(a => a.User)
            .Where(a => a.IsActive && a.StartDate <= now && (a.ExpiresAt == null || a.ExpiresAt > now))
            .Where(a =>
                // Company-wide (no targets)
                !a.AnnouncementTargets.Any() ||
                // Targeted: ALL
                a.AnnouncementTargets.Any(t => t.TargetType == "ALL") ||
                // Targeted: specific team
                (teamId != null && a.AnnouncementTargets.Any(t => t.TargetType == "TEAM" && t.TargetEntityId == teamId)) ||
                // Targeted: specific office
                (officeId != null && a.AnnouncementTargets.Any(t => t.TargetType == "OFFICE" && t.TargetEntityId == officeId))
            )
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PostDate)
            .Take(20)
            .Select(a => new
            {
                a.AnnouncementId,
                a.Title,
                a.AnnouncementText,
                AuthorName = a.Author.User.FirstName + " " + a.Author.User.LastName,
                a.PostDate,
                a.Priority,
                a.IsPinned,
                a.ViewCount
            })
            .ToListAsync();

        return ApiResults.Success(announcements);
    }
}
