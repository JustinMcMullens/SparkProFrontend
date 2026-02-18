using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;

namespace SparkBackend.Endpoints;

public static class AnnouncementEndpoints
{
    public static void MapAnnouncementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/announcements")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetAnnouncements);
        group.MapGet("/{id:int}", GetAnnouncementDetail);
        group.MapPost("/", CreateAnnouncement);
        group.MapPut("/{id:int}", UpdateAnnouncement);
        group.MapDelete("/{id:int}", DeleteAnnouncement);
        group.MapPost("/{id:int}/acknowledge", AcknowledgeAnnouncement);
        group.MapGet("/unread-count", GetUnreadCount);
    }

    private static async Task<IResult> GetAnnouncements(
        HttpContext http, SparkDbContext db,
        int? page, int? pageSize)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;
        var employee = await db.Employees
            .Include(e => e.Team)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

        var teamId = employee?.TeamId;
        var officeId = employee?.Team?.OfficeId;

        var query = db.Announcements
            .Include(a => a.AnnouncementTargets)
            .Where(a => a.IsActive && a.StartDate <= now && (a.ExpiresAt == null || a.ExpiresAt > now))
            .Where(a =>
                !a.AnnouncementTargets.Any() ||
                a.AnnouncementTargets.Any(t => t.TargetType == "ALL") ||
                (teamId != null && a.AnnouncementTargets.Any(t => t.TargetType == "TEAM" && t.TargetEntityId == teamId)) ||
                (officeId != null && a.AnnouncementTargets.Any(t => t.TargetType == "OFFICE" && t.TargetEntityId == officeId)))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PostDate)
            .Select(a => new
            {
                a.AnnouncementId,
                a.Title,
                a.AnnouncementText,
                a.PostDate,
                a.Priority,
                a.IsPinned,
                a.ViewCount,
                IsAcknowledged = a.AnnouncementViews.Any(v => v.UserId == currentUserId && v.IsAcknowledged)
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetAnnouncementDetail(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var announcement = await db.Announcements
            .Include(a => a.Author).ThenInclude(a => a.User)
            .Include(a => a.AnnouncementTargets)
            .FirstOrDefaultAsync(a => a.AnnouncementId == id);

        if (announcement == null) return ApiResults.NotFound("Announcement not found");

        // Record view
        var existingView = await db.AnnouncementViews
            .FirstOrDefaultAsync(v => v.AnnouncementId == id && v.UserId == currentUserId);

        if (existingView == null)
        {
            db.AnnouncementViews.Add(new AnnouncementView
            {
                AnnouncementId = id,
                UserId = currentUserId,
                ViewedAt = DateTime.UtcNow,
                IsAcknowledged = false
            });
            announcement.ViewCount++;
            await db.SaveChangesAsync();
        }

        return ApiResults.Success(new
        {
            announcement.AnnouncementId,
            announcement.Title,
            announcement.AnnouncementText,
            AuthorName = announcement.Author.User.FirstName + " " + announcement.Author.User.LastName,
            announcement.PostDate,
            announcement.StartDate,
            announcement.EndDate,
            announcement.ExpiresAt,
            announcement.Priority,
            announcement.Visibility,
            announcement.IsPinned,
            announcement.ViewCount,
            Targets = announcement.AnnouncementTargets.Select(t => new { t.TargetType, t.TargetEntityId }),
            IsAcknowledged = existingView?.IsAcknowledged ?? false
        });
    }

    private static async Task<IResult> CreateAnnouncement(
        HttpContext http, SparkDbContext db, CreateAnnouncementRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;
        var announcement = new Announcement
        {
            Title = request.Title,
            AnnouncementText = request.Body,
            AuthorId = currentUserId,
            PostDate = now,
            StartDate = now,
            ExpiresAt = request.ExpiresAt,
            Priority = request.Priority ?? "NORMAL",
            Visibility = "ALL",
            IsActive = true,
            IsPinned = false,
            ViewCount = 0,
            CreatedAt = now,
            CreatedBy = currentUserId,
            UpdatedAt = now
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        // Add targets if specified
        if (request.Targets?.Any() == true)
        {
            foreach (var target in request.Targets)
            {
                db.AnnouncementTargets.Add(new AnnouncementTarget
                {
                    AnnouncementId = announcement.AnnouncementId,
                    TargetType = target.TargetType,
                    TargetEntityId = target.TargetId
                });
            }
            await db.SaveChangesAsync();
        }

        return ApiResults.Success(new { announcement.AnnouncementId });
    }

    private static async Task<IResult> UpdateAnnouncement(
        int id, HttpContext http, SparkDbContext db, AnnouncementUpdateRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var announcement = await db.Announcements.FindAsync(id);
        if (announcement == null) return ApiResults.NotFound("Announcement not found");

        if (request.Title != null) announcement.Title = request.Title;
        if (request.Body != null) announcement.AnnouncementText = request.Body;
        if (request.Priority != null) announcement.Priority = request.Priority;
        if (request.ExpiresAt.HasValue) announcement.ExpiresAt = request.ExpiresAt;
        if (request.IsPinned.HasValue) announcement.IsPinned = request.IsPinned.Value;
        announcement.UpdatedAt = DateTime.UtcNow;
        announcement.UpdatedBy = currentUserId;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { announcement.AnnouncementId, Updated = true });
    }

    private static async Task<IResult> DeleteAnnouncement(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var announcement = await db.Announcements.FindAsync(id);
        if (announcement == null) return ApiResults.NotFound("Announcement not found");

        announcement.IsActive = false;
        announcement.UpdatedAt = DateTime.UtcNow;
        announcement.UpdatedBy = currentUserId;
        await db.SaveChangesAsync();

        return ApiResults.Success(new { announcement.AnnouncementId, Deleted = true });
    }

    private static async Task<IResult> AcknowledgeAnnouncement(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var view = await db.AnnouncementViews
            .FirstOrDefaultAsync(v => v.AnnouncementId == id && v.UserId == currentUserId);

        if (view == null)
        {
            db.AnnouncementViews.Add(new AnnouncementView
            {
                AnnouncementId = id,
                UserId = currentUserId,
                ViewedAt = DateTime.UtcNow,
                IsAcknowledged = true,
                AcknowledgedAt = DateTime.UtcNow
            });
        }
        else
        {
            view.IsAcknowledged = true;
            view.AcknowledgedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { AnnouncementId = id, Acknowledged = true });
    }

    private static async Task<IResult> GetUnreadCount(HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;
        var employee = await db.Employees
            .Include(e => e.Team)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.IsActive);

        var teamId = employee?.TeamId;
        var officeId = employee?.Team?.OfficeId;

        var acknowledgedIds = await db.AnnouncementViews
            .Where(v => v.UserId == currentUserId && v.IsAcknowledged)
            .Select(v => v.AnnouncementId)
            .ToListAsync();

        var unreadCount = await db.Announcements
            .Include(a => a.AnnouncementTargets)
            .Where(a => a.IsActive && a.StartDate <= now && (a.ExpiresAt == null || a.ExpiresAt > now))
            .Where(a => !acknowledgedIds.Contains(a.AnnouncementId))
            .Where(a =>
                !a.AnnouncementTargets.Any() ||
                a.AnnouncementTargets.Any(t => t.TargetType == "ALL") ||
                (teamId != null && a.AnnouncementTargets.Any(t => t.TargetType == "TEAM" && t.TargetEntityId == teamId)) ||
                (officeId != null && a.AnnouncementTargets.Any(t => t.TargetType == "OFFICE" && t.TargetEntityId == officeId)))
            .CountAsync();

        return ApiResults.Success(new { UnreadCount = unreadCount });
    }
}

public record AnnouncementUpdateRequest(
    string? Title, string? Body, string? Priority,
    DateTime? ExpiresAt, bool? IsPinned);
