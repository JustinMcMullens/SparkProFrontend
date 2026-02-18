using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetTickets);
        group.MapGet("/{id:int}", GetTicketDetail);
        group.MapPost("/", CreateTicket);
        group.MapPut("/{id:int}", UpdateTicket);
        group.MapPost("/{id:int}/comments", AddComment);
        group.MapPost("/{id:int}/status", ChangeStatus);
        group.MapPost("/{id:int}/assign", AssignTicket);
    }

    private static async Task<IResult> GetTickets(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        int? page, int? pageSize,
        string? status, string? priority, int? assignedTo)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var query = db.Tickets
            .WhereIf(!string.IsNullOrEmpty(status), t => t.Status == status)
            .WhereIf(!string.IsNullOrEmpty(priority), t => t.Priority == priority)
            .WhereIf(assignedTo.HasValue, t => t.AssignedTo == assignedTo!.Value)
            .AsQueryable();

        // Authority scoping
        if (authorityLevel <= 2)
            query = query.Where(t => t.CreatedBy == currentUserId || t.AssignedTo == currentUserId);
        else if (authorityLevel == 3)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(t => accessibleIds.Contains(t.CreatedBy ?? 0) || accessibleIds.Contains(t.AssignedTo ?? 0));
        }
        // Level 4+ sees all

        var projected = query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.TicketId,
                t.Subject,
                t.Priority,
                t.Status,
                t.SaleId,
                t.AssignedTo,
                t.CreatedBy,
                t.CreatedAt,
                t.ResolvedAt,
                t.ClosedAt
            });

        var result = await projected.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetTicketDetail(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out _);
        if (authResult != null) return authResult;

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return ApiResults.NotFound("Ticket not found");

        var comments = await db.TicketComments
            .Where(c => c.TicketId == id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.CommentId,
                c.CommentText,
                c.IsInternal,
                c.CreatedAt
            })
            .ToListAsync();

        var statusHistory = await db.TicketStatusHistories
            .Where(h => h.TicketId == id)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new
            {
                h.HistoryId,
                h.OldStatus,
                h.NewStatus,
                h.ChangedAt,
                h.ChangedBy
            })
            .ToListAsync();

        return ApiResults.Success(new
        {
            ticket.TicketId,
            ticket.Subject,
            ticket.Description,
            ticket.Priority,
            ticket.Status,
            ticket.SaleId,
            ticket.CustomerId,
            ticket.AssignedTo,
            ticket.CreatedBy,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ResolvedBy,
            ticket.ClosedAt,
            ticket.ClosedBy,
            Comments = comments,
            StatusHistory = statusHistory
        });
    }

    private static async Task<IResult> CreateTicket(
        HttpContext http, SparkDbContext db, CreateTicketRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Subject = request.Subject,
            Description = request.Description,
            Priority = request.Priority ?? "MEDIUM",
            Status = "OPEN",
            CreatedAt = now,
            CreatedBy = currentUserId,
            UpdatedAt = now
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Create initial status history
        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = ticket.TicketId,
            OldStatus = null,
            NewStatus = "OPEN",
            ChangedAt = now,
            ChangedBy = currentUserId
        });
        await db.SaveChangesAsync();

        return ApiResults.Success(new { ticket.TicketId });
    }

    private static async Task<IResult> UpdateTicket(
        int id, HttpContext http, SparkDbContext db, TicketUpdateRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return ApiResults.NotFound("Ticket not found");

        if (request.Subject != null) ticket.Subject = request.Subject;
        if (request.Description != null) ticket.Description = request.Description;
        if (request.Priority != null) ticket.Priority = request.Priority;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedBy = currentUserId;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { ticket.TicketId, Updated = true });
    }

    private static async Task<IResult> AddComment(
        int id, HttpContext http, SparkDbContext db, AddCommentRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out _);
        if (authResult != null) return authResult;

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return ApiResults.NotFound("Ticket not found");

        var comment = new TicketComment
        {
            TicketId = id,
            CommentText = request.Body,
            IsInternal = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.TicketComments.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ApiResults.Success(new { comment.CommentId });
    }

    private static async Task<IResult> ChangeStatus(
        int id, HttpContext http, SparkDbContext db, ChangeTicketStatusRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out var currentUserId);
        if (authResult != null) return authResult;

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return ApiResults.NotFound("Ticket not found");

        var oldStatus = ticket.Status;
        var newStatus = request.Status.ToUpper();
        var now = DateTime.UtcNow;

        ticket.Status = newStatus;
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = currentUserId;

        if (newStatus == "RESOLVED")
        {
            ticket.ResolvedAt = now;
            ticket.ResolvedBy = currentUserId;
        }
        else if (newStatus == "CLOSED")
        {
            ticket.ClosedAt = now;
            ticket.ClosedBy = currentUserId;
        }

        // Record status history
        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedBy = currentUserId
        });

        await db.SaveChangesAsync();
        return ApiResults.Success(new { ticket.TicketId, OldStatus = oldStatus, NewStatus = newStatus });
    }

    private static async Task<IResult> AssignTicket(
        int id, HttpContext http, SparkDbContext db, AssignTicketRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out var currentUserId);
        if (authResult != null) return authResult;

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket == null) return ApiResults.NotFound("Ticket not found");

        ticket.AssignedTo = request.AssignedUserId;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedBy = currentUserId;
        await db.SaveChangesAsync();

        return ApiResults.Success(new { ticket.TicketId, ticket.AssignedTo });
    }
}

public record TicketUpdateRequest(string? Subject, string? Description, string? Priority);
