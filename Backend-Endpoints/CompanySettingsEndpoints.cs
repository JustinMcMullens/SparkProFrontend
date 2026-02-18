using Microsoft.AspNetCore.Mvc;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Middleware;
using SparkBackend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace SparkBackend.Endpoints;

public static class CompanySettingsEndpoints
{
    public static void MapCompanySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings/company")
            .WithTags("Company Settings");

        group.MapGet("/", async (HttpContext context, SparkDbContext db) =>
        {
            var authResult = EndpointAuthHelpers.RequireAuth(context, out _);
            if (authResult != null) return authResult;

            var subdomain = TenantContext.GetSubdomain(context);

            var projectTypes = await db.ProjectTypes
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new { p.ProjectTypeId, p.ProjectTypeName, p.Description })
                .ToListAsync();

            var salesRoles = await db.SalesRoles
                .Where(r => r.IsActive)
                .Select(r => new { r.RoleId, r.RoleName, r.Description })
                .ToListAsync();

            var generationTypes = await db.GenerationTypes
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new { g.GenerationTypeId, g.GenerationTypeName, g.Description })
                .ToListAsync();

            var allocationTypes = await db.AllocationTypes
                .Select(a => new { a.AllocationTypeId, a.AllocationTypeName, a.Description })
                .ToListAsync();

            return ApiResults.Success(new
            {
                Subdomain = subdomain,
                ProjectTypes = projectTypes,
                SalesRoles = salesRoles,
                GenerationTypes = generationTypes,
                AllocationTypes = allocationTypes
            });
        })
        .RequireAuthorization("Authenticated")
        .WithName("GetCompanySettings");

        group.MapPut("/", async (HttpContext context, SparkDbContext db) =>
        {
            var authResult = EndpointAuthHelpers.RequireAuthority(context, 5, out var currentUserId);
            if (authResult != null) return authResult;

            // Company settings are stored in the tenant schema
            // For now, return success - actual settings table TBD
            return ApiResults.Success(new { Message = "Company settings updated" });
        })
        .RequireAuthorization("Level5")
        .WithName("UpdateCompanySettings");
    }
}
