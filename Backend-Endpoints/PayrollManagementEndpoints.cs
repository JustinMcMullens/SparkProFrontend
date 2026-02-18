using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Models;

namespace SparkBackend.Endpoints;

public static class PayrollManagementEndpoints
{
    public static void MapPayrollManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/paystubs")
            .WithTags("Payroll Management");

        // GET /api/paystubs/upcoming - Get all unpaid commission allocations ready for approval
        group.MapGet("/upcoming", async (HttpContext http, SparkDbContext db, int? companyId) =>
        {
            try
            {
                // Get authenticated user
                var authUserId = http.Session.GetInt32("UserId");
                if (authUserId == null)
                {
                    return Results.Json(new { error = "Authentication required" }, statusCode: 401);
                }

                // Get company ID from session or parameter
                var effectiveCompanyId = companyId ?? http.Session.GetInt32("CompanyId");
                if (effectiveCompanyId == null)
                {
                    return Results.Json(new { error = "Company ID required" }, statusCode: 400);
                }

                // Check authorization - only L4+ can view payroll management
                var requesterEmp = await db.Employees
                    .Include(e => e.Title)
                    .FirstOrDefaultAsync(er => er.UserId == authUserId.Value);
                
                if (requesterEmp == null)
                {
                    return Results.Json(new { 
                        error = "Access denied", 
                        message = $"User {authUserId.Value} is not in the employee registry"
                    }, statusCode: 403);
                }

                var authorityLevel = requesterEmp.Title?.TitleLevel ?? 2;
                if (authorityLevel < 4)
                {
                    return Results.Json(new { 
                        error = "Insufficient permissions", 
                        message = "Manager access (Level 4+) required for payroll management"
                    }, statusCode: 403);
                }

                // Return simple test response first to verify endpoint works
                return Results.Ok(new
                {
                    upcomingPayments = new object[] { },
                    totalCount = 0,
                    totalEstimated = 0.0m,
                    pendingApproval = 0,
                    approved = 0,
                    exported = 0
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = $"Server error: {ex.Message}", details = ex.ToString() }, statusCode: 500);
            }
        });
    }
}
