using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;

namespace SparkBackend.Endpoints;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goals")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetGoals);
        group.MapGet("/{id:int}", GetGoalDetail);
        group.MapGet("/my-progress", GetMyProgress);
        group.MapGet("/leaderboard/{leaderboardId:int}", GetLeaderboard);
        group.MapPost("/", CreateGoal);
        group.MapPut("/{id:int}", UpdateGoal);
    }

    private static async Task<IResult> GetGoals(
        HttpContext http,
        SparkDbContext db,
        int? page, int? pageSize,
        string? goalLevel, bool? isActive)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        // Get user's org context
        var employee = await db.Employees
            .Include(e => e.Team).ThenInclude(t => t!.Office)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

        var teamId = employee?.TeamId;
        var officeId = employee?.Team?.OfficeId;

        // Show goals relevant to this user's org hierarchy
        var query = db.Goals
            .WhereIf(isActive.HasValue, g => g.IsActive == isActive!.Value)
            .WhereIf(!string.IsNullOrEmpty(goalLevel), g => g.GoalLevel == goalLevel)
            .Where(g =>
                g.UserId == currentUserId ||
                (teamId != null && g.TeamId == teamId) ||
                (officeId != null && g.OfficeId == officeId) ||
                g.GoalLevel == "COMPANY")
            .OrderByDescending(g => g.StartDate)
            .Select(g => new
            {
                g.GoalId,
                g.GoalName,
                g.GoalLevel,
                g.TargetValue,
                g.CurrentValue,
                g.MinimumValue,
                g.StretchValue,
                g.StartDate,
                g.EndDate,
                g.IsActive,
                g.IsAchieved,
                g.IsStretchAchieved,
                ProgressPercent = g.TargetValue > 0 ? Math.Round(g.CurrentValue / g.TargetValue * 100, 1) : 0
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetGoalDetail(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out _);
        if (authResult != null) return authResult;

        var goal = await db.Goals
            .Include(g => g.GoalType)
            .Include(g => g.ProjectType)
            .FirstOrDefaultAsync(g => g.GoalId == id);

        if (goal == null) return ApiResults.NotFound("Goal not found");

        return ApiResults.Success(new
        {
            goal.GoalId,
            goal.GoalName,
            goal.GoalDescription,
            GoalType = goal.GoalType?.GoalTypeName,
            goal.GoalLevel,
            ProjectType = goal.ProjectType?.ProjectTypeName,
            goal.TargetValue,
            goal.CurrentValue,
            goal.MinimumValue,
            goal.StretchValue,
            goal.StartDate,
            goal.EndDate,
            goal.IsActive,
            goal.IsAchieved,
            goal.AchievedAt,
            goal.IsStretchAchieved,
            goal.StretchAchievedAt,
            goal.RegionId,
            goal.OfficeId,
            goal.TeamId,
            goal.UserId,
            ProgressPercent = goal.TargetValue > 0 ? Math.Round(goal.CurrentValue / goal.TargetValue * 100, 1) : 0
        });
    }

    private static async Task<IResult> GetMyProgress(HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var employee = await db.Employees
            .Include(e => e.Team).ThenInclude(t => t!.Office)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

        var teamId = employee?.TeamId;
        var officeId = employee?.Team?.OfficeId;

        var progress = await db.VGoalProgressSummaries
            .Where(g => g.IsActive == true &&
                (g.UserId == currentUserId ||
                 (teamId != null && g.TeamId == teamId) ||
                 (officeId != null && g.OfficeId == officeId) ||
                 g.GoalLevel == "COMPANY"))
            .OrderByDescending(g => g.ProgressPercent)
            .ToListAsync();

        return ApiResults.Success(progress);
    }

    private static async Task<IResult> GetLeaderboard(int leaderboardId, SparkDbContext db)
    {
        var leaderboard = await db.GoalLeaderboards
            .Include(l => l.GoalType)
            .FirstOrDefaultAsync(l => l.LeaderboardId == leaderboardId && l.IsActive);

        if (leaderboard == null) return ApiResults.NotFound("Leaderboard not found");

        // Find goals matching the leaderboard's config
        var goalsQuery = db.Goals
            .Where(g => g.GoalTypeId == leaderboard.GoalTypeId && g.IsActive && g.UserId != null);

        // Scope by leaderboard scope
        if (leaderboard.ScopeTeamId.HasValue)
            goalsQuery = goalsQuery.Where(g => g.TeamId == leaderboard.ScopeTeamId);
        else if (leaderboard.ScopeOfficeId.HasValue)
            goalsQuery = goalsQuery.Where(g => g.OfficeId == leaderboard.ScopeOfficeId);
        else if (leaderboard.ScopeRegionId.HasValue)
            goalsQuery = goalsQuery.Where(g => g.RegionId == leaderboard.ScopeRegionId);

        var ranked = await goalsQuery
            .OrderByDescending(g => g.CurrentValue)
            .Take(leaderboard.MaxDisplayCount ?? 25)
            .Select(g => new
            {
                g.UserId,
                g.GoalName,
                g.CurrentValue,
                g.TargetValue,
                ProgressPercent = g.TargetValue > 0 ? Math.Round(g.CurrentValue / g.TargetValue * 100, 1) : 0,
                g.IsAchieved
            })
            .ToListAsync();

        // Enrich with names
        var userIds = ranked.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).ToList();
        var users = await db.Employees
            .Where(e => userIds.Contains(e.UserId))
            .Include(e => e.User)
            .ToDictionaryAsync(e => e.UserId);

        var result = ranked.Select((r, i) => new
        {
            Rank = i + 1,
            r.UserId,
            FirstName = r.UserId.HasValue && users.TryGetValue(r.UserId.Value, out var emp) ? emp.User.FirstName : null,
            LastName = r.UserId.HasValue && users.TryGetValue(r.UserId.Value, out var emp2) ? emp2.User.LastName : null,
            r.CurrentValue,
            r.TargetValue,
            r.ProgressPercent,
            r.IsAchieved
        });

        return ApiResults.Success(new
        {
            leaderboard.LeaderboardId,
            leaderboard.LeaderboardName,
            GoalType = leaderboard.GoalType?.GoalTypeName,
            Rankings = result
        });
    }

    private static async Task<IResult> CreateGoal(
        HttpContext http, SparkDbContext db, CreateGoalRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var goal = new Goal
        {
            GoalName = request.GoalName,
            GoalTypeId = request.GoalTypeId,
            GoalLevel = request.GoalLevel,
            TargetValue = request.TargetValue,
            CurrentValue = 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TeamId = request.TeamId,
            OfficeId = request.OfficeId,
            RegionId = request.RegionId,
            IsActive = true,
            IsAchieved = false,
            IsStretchAchieved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            UpdatedAt = DateTime.UtcNow
        };

        db.Goals.Add(goal);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { goal.GoalId });
    }

    private static async Task<IResult> UpdateGoal(
        int id, HttpContext http, SparkDbContext db, GoalUpdateRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var goal = await db.Goals.FindAsync(id);
        if (goal == null) return ApiResults.NotFound("Goal not found");

        if (request.GoalName != null) goal.GoalName = request.GoalName;
        if (request.TargetValue.HasValue) goal.TargetValue = request.TargetValue.Value;
        if (request.MinimumValue.HasValue) goal.MinimumValue = request.MinimumValue;
        if (request.StretchValue.HasValue) goal.StretchValue = request.StretchValue;
        if (request.IsActive.HasValue) goal.IsActive = request.IsActive.Value;
        goal.UpdatedAt = DateTime.UtcNow;
        goal.UpdatedBy = currentUserId;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { goal.GoalId, Updated = true });
    }
}

public record GoalUpdateRequest(
    string? GoalName, decimal? TargetValue, decimal? MinimumValue,
    decimal? StretchValue, bool? IsActive);
