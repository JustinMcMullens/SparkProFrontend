using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Services;
using SparkBackend.Middleware;
using ImageMagick;
using System.Security.Claims;
using BCrypt.Net;
using static SparkBackend.Endpoints.EndpointAuthHelpers;

namespace SparkBackend.Endpoints;

public static class ReadEndpoints
{
    public static void MapReadEndpoints(this WebApplication app)
    {
        app.MapMethods("/health", new[] { "GET", "HEAD" }, () => Results.Ok("OK"));
        app.MapGet("/tenant-info", (HttpContext http) =>
        {
            var companyId = TenantContext.GetCompanyId(http);
            var subdomain = TenantContext.GetSubdomain(http);
            var companyName = TenantContext.GetCompanyName(http);
            
            return Results.Ok(new
            {
                Host = http.Request.Host.ToString(),
                DetectedCompanyId = companyId,
                DetectedSubdomain = subdomain,
                DetectedCompanyName = companyName
            });
        });
        
        app.MapGet("/companies", (HttpContext http) =>
        {
            var authResult = RequireAuth(http, out _);
            if (authResult != null) return authResult;

            var subdomain = TenantContext.GetSubdomain(http);
            var companyName = TenantContext.GetCompanyName(http);
            return Results.Ok(new[] { new { Subdomain = subdomain, Name = companyName ?? subdomain } });
        });
        
        app.MapGet("/{company_id:int}/employee_registry", async (int company_id, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuth(http, out _);
            if (authResult != null) return authResult;

            var tenantCompanyId = TenantContext.GetCompanyId(http);
            if (tenantCompanyId.HasValue && tenantCompanyId.Value != company_id)
            {
                return Results.Forbid();
            }

            try
            {
                // CompanyId filter removed - company context comes from schema isolation
                var employees = await db.Employees
                    .ToListAsync();
                return Results.Ok(employees);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching employee registry: {ex.Message}");
            }
        });
        
        app.MapGet("/{userId:int}/{company_id:int}/employee_directory", async (int userId, int company_id, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuth(http, out var authenticatedUserId);
            if (authResult != null) return authResult;

            var authorityLevel = http.Session.GetInt32("AuthorityLevel") ?? 1;
            if (authenticatedUserId != userId && authorityLevel < 4)
                return Results.Forbid();

            var tenantCompanyId = TenantContext.GetCompanyId(http);
            if (tenantCompanyId.HasValue && tenantCompanyId.Value != company_id)
            {
                return Results.Forbid();
            }

            var user = await db.Users.FindAsync(userId);
            if (user is null)
                return Results.NotFound("User not found");
        
            try
            {
                // CompanyId filter removed - company context comes from schema isolation
                var employees = await db.Employees
                    .Where(e => e.IsActive == true)
                    .Select(er => new
                    {
                        UserId = er.UserId,
                        EmployeeName = db.Users.Where(u => u.UserId == er.UserId)
                                            .Select(u => u.FirstName + " " + u.LastName)
                                            .FirstOrDefault(),
                        EmployeeTitle = er.Title,
                        EmployeeEmail = db.Users.Where(u => u.UserId == er.UserId)
                                            .Select(u => u.Email)
                                            .FirstOrDefault(),
                        EmployeeManagerId = er.ManagerId
                        
                    })
                    .ToListAsync();
                return Results.Ok(employees);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching employee registry: {ex.Message}");
            }
        });
        
        app.MapGet("/profile/{userId:int}", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            try
            {
                // Require authentication and verify ownership
                var authResult = RequireAuthAndOwnership(http, userId, out int authenticatedUserId);
                if (authResult != null)
                {
                    return authResult;
                }
                
                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Select(u => new 
                    {
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.Phone
                    })
                    .FirstOrDefaultAsync();
        
                // Fetch user media separately
                var userMedia = await db.UserMedia
                    .AsNoTracking()
                    .Where(um => um.UserId == userId)
                    .Select(um => new
                    {
                        um.ProfileImageUrl,
                        um.DashboardBackgroundUrl,
                        um.BannerImageUrl
                    })
                    .FirstOrDefaultAsync();
                    
                if (user == null)
                {
                    return Results.NotFound();
                }
        
                var employee = await db.Employees
                    .AsNoTracking()
                    .Include(e => e.Team)
                        .ThenInclude(t => t!.Office)
                            .ThenInclude(o => o!.Region)
                    .Include(e => e.Title)
                    .Where(e => e.UserId == userId)
                    .Select(e => new
                    {
                        AuthorityLevel = e.Title != null ? e.Title.TitleLevel : (int?)null,
                        Title = e.Title != null ? e.Title.TitleName : null,
                        e.Category,
                        RegionName = e.Team != null && e.Team.Office != null && e.Team.Office.Region != null ? e.Team.Office.Region.RegionName : null,
                        OfficeName = e.Team != null && e.Team.Office != null ? e.Team.Office.OfficeName : null,
                        e.CreatedAt
                    })
                    .FirstOrDefaultAsync();
        
                // Companies are now PostgreSQL schemas - get company name from tenant context
                var companyName = TenantContext.GetCompanyName(http);
        
                // Resolve defaults to server-hosted static assets under /user_images
                var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        
                string ToAbsolute(string? urlOrPath)
                    => string.IsNullOrWhiteSpace(urlOrPath)
                        ? string.Empty
                        : (urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? urlOrPath
                            : $"{baseUrl}{(urlOrPath.StartsWith('/') ? urlOrPath : "/" + urlOrPath)}");
        
                var profileImageUrl =
                    string.IsNullOrEmpty(userMedia?.ProfileImageUrl)
                    ? $"{baseUrl}/user_images/default_avatar.avif"
                    : ToAbsolute(userMedia.ProfileImageUrl);
        
                var dashboardBannerUrl =
                    string.IsNullOrEmpty(userMedia?.DashboardBackgroundUrl)
                    ? $"{baseUrl}/user_images/default_dashboard.avif"
                    : ToAbsolute(userMedia.DashboardBackgroundUrl);
        
                var profileBannerUrl =
                    string.IsNullOrEmpty(userMedia?.BannerImageUrl)
                    ? $"{baseUrl}/user_images/default_profile_banner.avif"
                    : ToAbsolute(userMedia.BannerImageUrl);
        
                return Results.Ok(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Phone,
                    employee?.AuthorityLevel,
                    employee?.Title,
                    employee?.Category,
                    Region = employee?.RegionName,
                    Office = employee?.OfficeName,
                    employee?.CreatedAt,
                    CompanyName = companyName,
        
                    // Resolved image URLs
                    ProfileImageUrl = profileImageUrl,
                    DashboardBannerUrl = dashboardBannerUrl,
                    ProfileBannerUrl = profileBannerUrl
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error loading profile: {ex.Message}");
            }
        });
    }
}
