using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Middleware;

namespace SparkBackend.Endpoints;

// TODO: This endpoint needs significant refactoring for the new schema
// PayrollBatch is company-wide, commission allocations link to batches via PayrollBatchId
// Paystubs per user should query salary_payouts and commission allocations by user

public static class PaystubEndpoints
{
    public static void MapPaystubEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/paystubs")
            .WithTags("Paystubs");

        // GET /api/paystubs - Get paystub history for current user
        group.MapGet("/", async (HttpContext http, SparkDbContext db) =>
        {
            try
            {
                var userId = http.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Results.Json(new { error = "Authentication required" }, statusCode: 401);
                }

                // Get users salary payouts as paystub history
                var salaryPayouts = await db.SalaryPayouts
                    .Where(sp => sp.UserId == userId.Value)
                    .OrderByDescending(sp => sp.PayoutDate)
                    .Take(12)
                    .Select(sp => new
                    {
                        PayoutId = sp.PayoutId,
                        PayoutDate = sp.PayoutDate,
                        PayoutAmount = sp.PayoutAmount,
                        IsPaid = sp.IsPaid,
                        PaidAt = sp.PaidAt,
                        HoursWorked = sp.HoursWorked,
                        OvertimeHoursWorked = sp.OvertimeHoursWorked
                    })
                    .ToListAsync();

                return Results.Ok(new { paystubs = salaryPayouts });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = $"Server error: {ex.Message}" }, statusCode: 500);
            }
        })
        .WithName("GetPaystubs")
        .WithDescription("Get paystub history for current user");

        // GET /api/paystubs/commissions - Get commission summary for current user
        group.MapGet("/commissions", async (HttpContext http, SparkDbContext db) =>
        {
            try
            {
                var userId = http.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Results.Json(new { error = "Authentication required" }, statusCode: 401);
                }

                // Get recent paid commissions by type
                var roofingCommissions = await db.RoofingCommissionAllocations
                    .Where(a => a.UserId == userId.Value && a.IsPaid)
                    .OrderByDescending(a => a.PaidAt)
                    .Take(10)
                    .Select(a => new { Type = "Roofing", Amount = a.AllocatedAmount, PaidAt = a.PaidAt })
                    .ToListAsync();

                var solarCommissions = await db.SolarCommissionAllocations
                    .Where(a => a.UserId == userId.Value && a.IsPaid)
                    .OrderByDescending(a => a.PaidAt)
                    .Take(10)
                    .Select(a => new { Type = "Solar", Amount = a.AllocatedAmount, PaidAt = a.PaidAt })
                    .ToListAsync();

                var fiberCommissions = await db.FiberCommissionAllocations
                    .Where(a => a.UserId == userId.Value && a.IsPaid)
                    .OrderByDescending(a => a.PaidAt)
                    .Take(10)
                    .Select(a => new { Type = "Fiber", Amount = a.AllocatedAmount, PaidAt = a.PaidAt })
                    .ToListAsync();

                var pestCommissions = await db.PestCommissionAllocations
                    .Where(a => a.UserId == userId.Value && a.IsPaid)
                    .OrderByDescending(a => a.PaidAt)
                    .Take(10)
                    .Select(a => new { Type = "Pest", Amount = a.AllocatedAmount, PaidAt = a.PaidAt })
                    .ToListAsync();

                return Results.Ok(new
                {
                    roofing = roofingCommissions,
                    solar = solarCommissions,
                    fiber = fiberCommissions,
                    pest = pestCommissions
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = $"Server error: {ex.Message}" }, statusCode: 500);
            }
        })
        .WithName("GetCommissionHistory")
        .WithDescription("Get paid commission history for current user");

        // Other endpoints return 501 for now
        group.MapGet("/summary", () => Results.Json(new { error = "Endpoint needs refactoring for new schema" }, statusCode: 501));
        group.MapGet("/pending", () => Results.Json(new { error = "Endpoint needs refactoring for new schema" }, statusCode: 501));
    }
}
