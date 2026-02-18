using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class PayrollEndpoints
{
    // Valid state transitions
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["DRAFT"] = ["SUBMITTED", "CANCELLED"],
        ["SUBMITTED"] = ["APPROVED", "CANCELLED"],
        ["APPROVED"] = ["EXPORTED", "CANCELLED"],
        ["EXPORTED"] = ["PAID", "CANCELLED"],
    };

    public static void MapPayrollEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payroll")
            .RequireAuthorization("Level4Plus");

        group.MapGet("/batches", GetBatches);
        group.MapGet("/batches/{id:int}", GetBatchDetail);
        group.MapPost("/batches", CreateBatch);
        group.MapPut("/batches/{id:int}", UpdateBatch);
        group.MapPost("/batches/{id:int}/add-allocations", AddAllocationsToBatch);
        group.MapPost("/batches/{id:int}/submit", SubmitBatch);
        group.MapPost("/batches/{id:int}/approve", ApproveBatch);
        group.MapPost("/batches/{id:int}/export", ExportBatch);
        group.MapPost("/batches/{id:int}/mark-paid", MarkBatchPaid);
    }

    private static async Task<IResult> GetBatches(
        SparkDbContext db, int? page, int? pageSize, string? status)
    {
        var query = db.PayrollBatches
            .WhereIf(!string.IsNullOrEmpty(status), b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.BatchId,
                b.BatchName,
                b.PayPeriodStart,
                b.PayPeriodEnd,
                b.PayDate,
                b.Status,
                b.TotalAmount,
                b.RecordCount,
                b.CreatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetBatchDetail(int id, SparkDbContext db, AllocationQueryService allocations)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return ApiResults.NotFound("Batch not found");

        // Get allocations in this batch across all industries
        var batchAllocations = await allocations.GetAllAllocations()
            .Where(a => a.PayrollBatchId == id)
            .ToListAsync();

        var overrides = await db.OverrideAllocations
            .Where(o => o.PayrollBatchId == id)
            .Select(o => new
            {
                o.AllocationId, o.SaleId, o.UserId, o.OverrideLevel,
                o.AllocatedAmount, o.IsApproved, o.IsPaid
            })
            .ToListAsync();

        return ApiResults.Success(new
        {
            batch.BatchId,
            batch.BatchName,
            batch.PayPeriodStart,
            batch.PayPeriodEnd,
            batch.PayDate,
            batch.Status,
            batch.TotalAmount,
            batch.RecordCount,
            batch.SubmittedAt,
            batch.ApprovedAt,
            batch.ExportedAt,
            batch.CreatedAt,
            Allocations = batchAllocations,
            Overrides = overrides
        });
    }

    private static async Task<IResult> CreateBatch(
        HttpContext http, SparkDbContext db, CreateBatchRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var batch = new PayrollBatch
        {
            BatchName = request.Description ?? $"Batch {DateTime.UtcNow:yyyy-MM-dd}",
            PayPeriodStart = request.PeriodStart ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
            PayPeriodEnd = request.PeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PayDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Status = "DRAFT",
            TotalAmount = 0,
            RecordCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            UpdatedAt = DateTime.UtcNow
        };

        db.PayrollBatches.Add(batch);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { batch.BatchId, batch.Status });
    }

    private static async Task<IResult> UpdateBatch(
        int id, HttpContext http, SparkDbContext db, CreateBatchRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return ApiResults.NotFound("Batch not found");
        if (batch.Status != "DRAFT") return ApiResults.BadRequest("Can only update DRAFT batches");

        if (request.Description != null) batch.BatchName = request.Description;
        if (request.PeriodStart.HasValue) batch.PayPeriodStart = request.PeriodStart.Value;
        if (request.PeriodEnd.HasValue) batch.PayPeriodEnd = request.PeriodEnd.Value;
        batch.UpdatedAt = DateTime.UtcNow;
        batch.UpdatedBy = currentUserId;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { batch.BatchId, Updated = true });
    }

    private static async Task<IResult> AddAllocationsToBatch(
        int id, HttpContext http, SparkDbContext db, AddAllocationsToBatchRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return ApiResults.NotFound("Batch not found");
        if (batch.Status != "DRAFT") return ApiResults.BadRequest("Can only add allocations to DRAFT batches");

        var added = 0;
        foreach (var item in request.Allocations)
        {
            switch (item.Industry.ToLower())
            {
                case "solar":
                    var solar = await db.SolarCommissionAllocations.FindAsync(item.AllocationId);
                    if (solar != null && solar.IsApproved && !solar.IsPaid)
                    { solar.PayrollBatchId = id; solar.UpdatedAt = DateTime.UtcNow; added++; }
                    break;
                case "pest":
                    var pest = await db.PestCommissionAllocations.FindAsync(item.AllocationId);
                    if (pest != null && pest.IsApproved && !pest.IsPaid)
                    { pest.PayrollBatchId = id; pest.UpdatedAt = DateTime.UtcNow; added++; }
                    break;
                case "roofing":
                    var roofing = await db.RoofingCommissionAllocations.FindAsync(item.AllocationId);
                    if (roofing != null && roofing.IsApproved && !roofing.IsPaid)
                    { roofing.PayrollBatchId = id; roofing.UpdatedAt = DateTime.UtcNow; added++; }
                    break;
                case "fiber":
                    var fiber = await db.FiberCommissionAllocations.FindAsync(item.AllocationId);
                    if (fiber != null && fiber.IsApproved && !fiber.IsPaid)
                    { fiber.PayrollBatchId = id; fiber.UpdatedAt = DateTime.UtcNow; added++; }
                    break;
            }
        }

        // Recalculate batch totals
        await RecalculateBatchTotals(db, batch);
        await db.SaveChangesAsync();

        return ApiResults.Success(new { batch.BatchId, Added = added, batch.TotalAmount, batch.RecordCount });
    }

    private static async Task<IResult> SubmitBatch(int id, HttpContext http, SparkDbContext db)
    {
        return await TransitionBatch(id, http, db, "DRAFT", "SUBMITTED");
    }

    private static async Task<IResult> ApproveBatch(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 5, out _);
        if (authResult != null) return authResult;

        return await TransitionBatch(id, http, db, "SUBMITTED", "APPROVED");
    }

    private static async Task<IResult> ExportBatch(int id, HttpContext http, SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 5, out _);
        if (authResult != null) return authResult;

        return await TransitionBatch(id, http, db, "APPROVED", "EXPORTED");
    }

    private static async Task<IResult> MarkBatchPaid(
        int id, HttpContext http, SparkDbContext db, AllocationQueryService allocations)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 5, out var currentUserId);
        if (authResult != null) return authResult;

        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return ApiResults.NotFound("Batch not found");
        if (batch.Status != "EXPORTED") return ApiResults.BadRequest("Batch must be EXPORTED to mark as PAID");

        var now = DateTime.UtcNow;
        batch.Status = "PAID";
        batch.UpdatedAt = now;
        batch.UpdatedBy = currentUserId;

        // Mark all allocations in batch as paid across all industries
        var solarAllocations = await db.SolarCommissionAllocations.Where(a => a.PayrollBatchId == id).ToListAsync();
        foreach (var a in solarAllocations) { a.IsPaid = true; a.PaidAt = now; a.UpdatedAt = now; }

        var pestAllocations = await db.PestCommissionAllocations.Where(a => a.PayrollBatchId == id).ToListAsync();
        foreach (var a in pestAllocations) { a.IsPaid = true; a.PaidAt = now; a.UpdatedAt = now; }

        var roofingAllocations = await db.RoofingCommissionAllocations.Where(a => a.PayrollBatchId == id).ToListAsync();
        foreach (var a in roofingAllocations) { a.IsPaid = true; a.PaidAt = now; a.UpdatedAt = now; }

        var fiberAllocations = await db.FiberCommissionAllocations.Where(a => a.PayrollBatchId == id).ToListAsync();
        foreach (var a in fiberAllocations) { a.IsPaid = true; a.PaidAt = now; a.UpdatedAt = now; }

        var overrideAllocations = await db.OverrideAllocations.Where(o => o.PayrollBatchId == id).ToListAsync();
        foreach (var o in overrideAllocations) { o.IsPaid = true; o.PaidAt = now; o.UpdatedAt = now; }

        await db.SaveChangesAsync();

        var totalMarked = solarAllocations.Count + pestAllocations.Count +
            roofingAllocations.Count + fiberAllocations.Count + overrideAllocations.Count;

        return ApiResults.Success(new { batch.BatchId, Status = "PAID", TotalMarkedPaid = totalMarked });
    }

    private static async Task<IResult> TransitionBatch(
        int id, HttpContext http, SparkDbContext db, string expectedStatus, string newStatus)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return ApiResults.NotFound("Batch not found");
        if (batch.Status != expectedStatus)
            return ApiResults.BadRequest($"Batch must be {expectedStatus} to transition to {newStatus}");

        var now = DateTime.UtcNow;
        batch.Status = newStatus;
        batch.UpdatedAt = now;
        batch.UpdatedBy = currentUserId;

        switch (newStatus)
        {
            case "SUBMITTED": batch.SubmittedAt = now; batch.SubmittedBy = currentUserId; break;
            case "APPROVED": batch.ApprovedAt = now; batch.ApprovedBy = currentUserId; break;
            case "EXPORTED": batch.ExportedAt = now; batch.ExportedBy = currentUserId; break;
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { batch.BatchId, batch.Status });
    }

    private static async Task RecalculateBatchTotals(SparkDbContext db, PayrollBatch batch)
    {
        var solarTotal = await db.SolarCommissionAllocations
            .Where(a => a.PayrollBatchId == batch.BatchId).SumAsync(a => a.AllocatedAmount);
        var pestTotal = await db.PestCommissionAllocations
            .Where(a => a.PayrollBatchId == batch.BatchId).SumAsync(a => a.AllocatedAmount);
        var roofingTotal = await db.RoofingCommissionAllocations
            .Where(a => a.PayrollBatchId == batch.BatchId).SumAsync(a => a.AllocatedAmount);
        var fiberTotal = await db.FiberCommissionAllocations
            .Where(a => a.PayrollBatchId == batch.BatchId).SumAsync(a => a.AllocatedAmount);
        var overrideTotal = await db.OverrideAllocations
            .Where(o => o.PayrollBatchId == batch.BatchId).SumAsync(o => o.AllocatedAmount);

        var solarCount = await db.SolarCommissionAllocations.CountAsync(a => a.PayrollBatchId == batch.BatchId);
        var pestCount = await db.PestCommissionAllocations.CountAsync(a => a.PayrollBatchId == batch.BatchId);
        var roofingCount = await db.RoofingCommissionAllocations.CountAsync(a => a.PayrollBatchId == batch.BatchId);
        var fiberCount = await db.FiberCommissionAllocations.CountAsync(a => a.PayrollBatchId == batch.BatchId);
        var overrideCount = await db.OverrideAllocations.CountAsync(o => o.PayrollBatchId == batch.BatchId);

        batch.TotalAmount = solarTotal + pestTotal + roofingTotal + fiberTotal + overrideTotal;
        batch.RecordCount = solarCount + pestCount + roofingCount + fiberCount + overrideCount;
    }
}
