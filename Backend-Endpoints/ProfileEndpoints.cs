using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Services;
using SparkBackend.Helpers;
using SparkBackend.Middleware;
using ImageMagick;
using System.Security.Claims;
using BCrypt.Net;
using static SparkBackend.Endpoints.EndpointAuthHelpers;

namespace SparkBackend.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        // ========== PROFILE UPDATE ENDPOINTS ==========
        
        app.MapPost("/profile/{userId:int}/images", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            // Require authentication and verify ownership (or admin)
            var authResult = RequireAuthAndOwnership(http, userId, out int authenticatedUserId);
            if (authResult != null)
            {
                // Log auth failure for debugging
                var sessionUserId = http.Session.GetInt32("UserId");
                var authorityLevel = http.Session.GetInt32("AuthorityLevel");
                Console.WriteLine($"[AUTH FAIL] Image upload: SessionUserId={sessionUserId}, RequestedUserId={userId}, AuthorityLevel={authorityLevel}");
                return authResult;
            }
        
            try
            {
                var form = await http.Request.ReadFormAsync();
                var file = form.Files.GetFile("file");
                var kind = form["kind"].ToString().Trim().ToLowerInvariant(); // "profile" | "profilebanner" | "dashboardbanner"
        
                if (file is null || file.Length == 0)
                    return Results.BadRequest(new { error = "No file provided" });
        
                // Basic size guard (we re-encode anyway)
                if (file.Length > 10 * 1024 * 1024)
                    return Results.BadRequest(new { error = "Max size 10MB" });
        
                // Map "kind" to a fixed base filename
                var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["profile"]         = "profile_picture",
                    ["profilebanner"]   = "profile_background",
                    ["dashboardbanner"] = "dashboard_image"
                };
                if (!nameMap.TryGetValue(kind, out var baseName))
                    return Results.BadRequest(new { error = "Invalid kind. Use profile, profileBanner, or dashboardBanner" });
        
                // Create root images directory if it doesn't exist
                var rootImagesDir = @"D:\images\user_images";
                if (!Directory.Exists(rootImagesDir))
                {
                    try
                    {
                        Directory.CreateDirectory(rootImagesDir);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Failed to create root images directory: {ex.Message}");
                    }
                }
        
                // Create user-specific folder if it doesn't exist
                var userFolder = Path.Combine(rootImagesDir, userId.ToString());
                if (!Directory.Exists(userFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(userFolder);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Failed to create user folder: {ex.Message}");
                    }
                }
        
                // Check for and delete any existing images for this type
                var existingFiles = Directory.Exists(userFolder) 
                    ? Directory.EnumerateFiles(userFolder, baseName + ".*").ToList()
                    : new List<string>();
        
                foreach (var existing in existingFiles)
                {
                    try 
                    { 
                        System.IO.File.Delete(existing);
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - not critical if old file deletion fails
                        Console.WriteLine($"Warning: Failed to delete old file {existing}: {ex.Message}");
                    }
                }
        
                var destPath = Path.Combine(userFolder, baseName + ".avif");
        
                // Decode -> optional resize -> encode to AVIF using ImageMagick
                using (var input = file.OpenReadStream())
                using (var img = new MagickImage(input))
                {
                    const int MAX = 2048;
                    if (img.Width > MAX || img.Height > MAX)
                    {
                        img.Resize(new MagickGeometry(MAX, MAX)
                        {
                            IgnoreAspectRatio = false,
                            Greater = true,
                            Less = false
                        });
                    }
                    img.Strip();
        
                    img.Format = MagickFormat.Avif;
                    img.Quality = 55;
                    img.Settings.SetDefine(MagickFormat.Heic, "speed", "6");
        
                    try
                    {
                        await img.WriteAsync(destPath);
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem($"Failed to save image: {ex.Message}");
                    }
                }
        
                // Public URL via StaticFileOptions mapping
                var publicPath = $"/user_images/{userId}/{baseName}.avif";
                var absoluteUrl = $"{http.Request.Scheme}://{http.Request.Host}{publicPath}";
        
                // Update user media record (or create if doesn't exist)
                var userMedia = await db.UserMedia.FindAsync(userId);
                if (userMedia == null)
                {
                    userMedia = new UserMedium { UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    db.UserMedia.Add(userMedia);
                }
        
                // Update the appropriate URL field based on kind
                switch (kind)
                {
                    case "profile":         userMedia.ProfileImageUrl = publicPath; break;
                    case "profilebanner":   userMedia.BannerImageUrl = publicPath; break;
                    case "dashboardbanner": userMedia.DashboardBackgroundUrl = publicPath; break;
                }
        
                userMedia.UpdatedAt = DateTime.UtcNow;
        
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to update user record: {ex.Message}");
                }
        
                return Results.Ok(new 
                { 
                    message = "Uploaded successfully", 
                    kind, 
                    url = publicPath, absoluteUrl
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Upload failed: {ex.Message}");
            }
        });
        
        // Get all companies a user belongs to (for company switcher)
        // NOTE: With schema isolation, this endpoint needs architectural review.
        // For now, returning the user's current company from the User table.
        app.MapGet("/api/user/{userId:int}/companies", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            // Require authentication and verify ownership
            var authResult = RequireAuthAndOwnership(http, userId, out int authenticatedUserId);
            if (authResult != null) return authResult;
            
            try
            {
                // Get user (verify exists)
                var user = await db.Users.FindAsync(userId);
                if (user == null)
                    return Results.NotFound($"User {userId} not found");
                    
                // Companies are now PostgreSQL schemas - get from tenant context
                var companyName = TenantContext.GetCompanyName(http);
                var subdomain = TenantContext.GetSubdomain(http);
                
                if (string.IsNullOrEmpty(companyName))
                    return Results.NotFound($"User {userId} does not belong to any companies");
        
                var employeeRecord = await db.Employees
                    .FirstOrDefaultAsync(er => er.UserId == userId && er.IsActive);
        
                var userCompanies = new[]
                {
                    new
                    {
                        CompanyId = 1, // Schema isolation means single company context
                        CompanyName = companyName,
                        Subdomain = subdomain ?? companyName.ToLower().Replace(" ", ""),
                        LogoUrl = (string?)null,
                        Role = employeeRecord?.Title?.TitleName ?? "Employee",
                        IsActive = employeeRecord?.IsActive ?? true,
                        JoinedAt = employeeRecord?.CreatedAt ?? user.CreatedAt
                    }
                };
        
                return Results.Ok(new
                {
                    UserId = userId,
                    Companies = userCompanies,
                    Count = userCompanies.Length
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error fetching user companies: {ex.Message}");
            }
        });
        
        // Update user's last accessed company (for tracking switches)
        app.MapPut("/api/user/{userId:int}/last-company", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            // Require authentication and verify ownership
            var authResult = RequireAuthAndOwnership(http, userId, out int authenticatedUserId);
            if (authResult != null) return authResult;
            
            try
            {
                var request = await http.Request.ReadFromJsonAsync<UpdateLastCompanyRequest>();
                if (request == null || request.CompanyId <= 0)
                    return Results.BadRequest("Valid CompanyId is required");
        
                // Verify user exists
                var user = await db.Users.FindAsync(userId);
                if (user == null)
                    return Results.NotFound($"User {userId} not found");
        
                // Verify user has an active employee record
                var employeeRecord = await db.Employees
                    .FirstOrDefaultAsync(er => er.UserId == userId && er.IsActive);
                
                if (employeeRecord == null)
                    return Results.Forbid(); // User doesn't have an active employee record
        
                // Update user's company (this will be their "last accessed" company)
                // CompanyId no longer exists on User - companies are now PostgreSQL schemas
                // This endpoint needs architectural review for multi-tenant schema approach
                user.UpdatedAt = DateTime.UtcNow;
                
                await db.SaveChangesAsync();
        
                return Results.Ok(new
                {
                    Message = "Company switched successfully",
                    UserId = userId,
                    NewCompanyId = request.CompanyId
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error updating last company: {ex.Message}");
            }
        });
        
        app.MapGet("/managedUsers/{userId:int}", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuth(http, out var authenticatedUserId);
            if (authResult != null) return authResult;

            var authorityLevel = http.Session.GetInt32("AuthorityLevel") ?? 1;
            if (authenticatedUserId != userId && authorityLevel < 4)
                return Results.Forbid();
            
            var team = await db.Employees
                .Where(er => er.ManagerId == userId && er.IsActive)
                .Include(er => er.User)
                .Include(er => er.Team)
                    .ThenInclude(t => t!.Office)
                        .ThenInclude(o => o!.Region)
                .ToListAsync();
        
            var results = new List<object>();
        
            // Default image for list avatars served from API static files
            var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
            var defaultListAvatar = $"{baseUrl}/user_images/default_avatar.avif";
        
            foreach (var member in team)
            {
                // ReferredBy no longer exists on Employee - skip recruiter lookup
                string? recruitedBy = null;
        
                // Calculate overrides - AllocatedAmount is the calculated amount, IsPaid is boolean
                var allocations = await db.RoofingCommissionAllocations
                    .Where(ca => ca.UserId == member.UserId)
                    .ToListAsync();
        
                var totalOverride = allocations.Sum(ca => ca.AllocatedAmount);
                var paidOverride = allocations.Where(ca => ca.IsPaid).Sum(ca => ca.AllocatedAmount);
        
                string ToAbsolute(string? urlOrPath)
                    => string.IsNullOrWhiteSpace(urlOrPath)
                        ? string.Empty
                        : (urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? urlOrPath
                            : $"{baseUrl}{(urlOrPath.StartsWith('/') ? urlOrPath : "/" + urlOrPath)}");
        
                // Get user media for profile image
                var memberMedia = await db.UserMedia
                    .Where(um => um.UserId == member.UserId)
                    .Select(um => um.ProfileImageUrl)
                    .FirstOrDefaultAsync();
        
                var resolvedProfile = string.IsNullOrEmpty(memberMedia)
                    ? defaultListAvatar
                    : ToAbsolute(memberMedia);
        
                results.Add(new
                {
                    member.User.UserId,
                    member.User.FirstName,
                    member.User.LastName,
                    Office = member.Team?.Office?.OfficeName,
                    Region = member.Team?.Office?.Region?.RegionName,
                    Team = member.Team?.TeamName ?? "N/A",
                    member.User.Email,
                    member.User.Phone,
                    member.ManagerId,
                    ProfilePicture = resolvedProfile,
                    Role = member.Title?.TitleName,
                    Status = member.IsActive ? "Active" : "Inactive",
                    StartDate = member.CreatedAt,
                    RecruitedBy = recruitedBy,
                    TotalOverride = totalOverride,
                    PaidOverride = paidOverride
                });
            }
        
            return Results.Ok(results);
        });
        
        app.MapGet("/referrals/{userId:int}", async (int userId, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuthAndOwnership(http, userId, out int authenticatedUserId);
            if (authResult != null) return authResult;
            
            // ReferredBy no longer exists on Employee - this endpoint needs architectural review
            // For now, return empty list
            var referred = new List<Employee>();
        
            var results = new List<object>();
        
            // Default image for list avatars served from API static files
            var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
            var defaultListAvatar = $"{baseUrl}/user_images/default_avatar.avif";
        
            foreach (var member in referred)
            {
                var recruiter = await db.Users.FindAsync(userId);
                string recruitedBy = recruiter != null
                                      ? $"{recruiter.FirstName} {recruiter.LastName}"
                                      : "Unknown Recruiter";
        
                // Calculate overrides - AllocatedAmount is the calculated amount, IsPaid is boolean
                var refAllocations = await db.RoofingCommissionAllocations
                    .Where(ca => ca.UserId == member.UserId)
                    .ToListAsync();
        
                var totalOverride = refAllocations.Sum(ca => ca.AllocatedAmount);
                var paidOverride = refAllocations.Where(ca => ca.IsPaid).Sum(ca => ca.AllocatedAmount);
        
                string ToAbsolute(string? urlOrPath)
                    => string.IsNullOrWhiteSpace(urlOrPath)
                        ? string.Empty
                        : (urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? urlOrPath
                            : $"{baseUrl}{(urlOrPath.StartsWith('/') ? urlOrPath : "/" + urlOrPath)}");
        
                // Get user media for profile image
                var refMemberMedia = await db.UserMedia
                    .Where(um => um.UserId == member.UserId)
                    .Select(um => um.ProfileImageUrl)
                    .FirstOrDefaultAsync();
        
                var resolvedProfile = string.IsNullOrEmpty(refMemberMedia)
                    ? defaultListAvatar
                    : ToAbsolute(refMemberMedia);
        
                results.Add(new
                {
                    member.User.UserId,
                    member.User.FirstName,
                    member.User.LastName,
                    Office = member.Team?.Office?.OfficeName,
                    Region = member.Team?.Office?.Region?.RegionName,
                    Team = member.Team?.TeamName,
                    member.User.Email,
                    member.User.Phone,
                    member.ManagerId,
                    ProfilePicture = resolvedProfile,
                    Role = member.Title?.TitleName,
                    Status = member.IsActive ? "Active" : "Inactive",
                    StartDate = member.CreatedAt,
                    RecruitedBy = recruitedBy,
                    TotalOverride = totalOverride,
                    PaidOverride = paidOverride
                });
            }
        
            return Results.Ok(results);
        });
        
        app.MapGet("/payroll/{userId:int}", async (HttpContext http, SparkDbContext db, int userId) =>
        {
            var authResult = RequireAuth(http, out var authenticatedUserId);
            if (authResult != null) return authResult;

            var authorityLevel = http.Session.GetInt32("AuthorityLevel") ?? 1;
            if (authenticatedUserId != userId && authorityLevel < 4)
                return Results.Forbid();
            
            // Scope: user + direct reports from employee_registry
            var managedUserIds = await db.Employees
                .AsNoTracking()
                .Where(er => er.ManagerId == userId)
                .Select(er => er.UserId)
                .ToListAsync();
        
            var scopeUserIds = managedUserIds
                .Append(userId)
                .Distinct()
                .ToList();
        
            // Base query: any sale with a participant in scope
            var items = await db.Sales
                .AsNoTracking()
                .Where(s => s.SaleParticipants.Any(p => scopeUserIds.Contains(p.UserId)))
                .Select(s => new
                {
                    s.SaleId,
                    s.CreatedAt,
                    UpdatedAt = (DateTime?)(s.UpdatedAt) ?? s.CreatedAt,
                    CustomerFirstName = s.Customer.FirstName,
                    CustomerLastName  = s.Customer.LastName,
                    ProjectType       = s.ProjectType.ProjectTypeName,
                    Stage             = s.SaleStatus,
                    Status            = s.SaleStatus,
                    SignedDate = (s.SolarSale != null && s.SolarSale.SignedDate.HasValue ? (DateTime?)s.SolarSale.SignedDate.Value.ToDateTime(TimeOnly.MinValue) : null)
                              ?? (s.RoofingSale != null && s.RoofingSale.SignedDate.HasValue ? (DateTime?)s.RoofingSale.SignedDate.Value.ToDateTime(TimeOnly.MinValue) : null)
                              ?? (s.FiberSale != null && s.FiberSale.SignedDate.HasValue ? (DateTime?)s.FiberSale.SignedDate.Value.ToDateTime(TimeOnly.MinValue) : null)
                              ?? (DateTime?)s.CreatedAt,
                    Participants = s.SaleParticipants.Select(p => new
                    {
                        p.UserId,
                        FirstName = p.User != null && p.User.User != null ? p.User.User.FirstName : "",
                        LastName  = p.User != null && p.User.User != null ? p.User.User.LastName : "",
                        p.SplitPercent,
                        RoleName  = p.Role.RoleName
                    }).ToList()
                })
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync();
        
            // Split participants into setters vs closers
            static bool IsSetter(string? roleName)
                => roleName != null && roleName.Equals("Setter", StringComparison.OrdinalIgnoreCase);
            static bool IsCloser(string? roleName)
                => roleName != null && roleName.Equals("Closer", StringComparison.OrdinalIgnoreCase);
        
            var shaped = items.Select(x =>
                new PayrollDealWithParticipantsDto(
                    x.SaleId,
                    x.CustomerFirstName ?? "Unknown",
                    x.CustomerLastName ?? "Customer",
                    x.ProjectType ?? "N/A",
                    x.Stage ?? "N/A",
                    x.Status ?? "Unknown",
                    x.SignedDate,
                    x.UpdatedAt,
                    x.CreatedAt,
                    Setters: x.Participants
                        .Where(p => IsSetter(p.RoleName))
                        .Select(p => new ParticipantDto(p.UserId, p.FirstName ?? "", p.LastName ?? "", p.SplitPercent ?? 0m))
                        .ToList(),
                    Closers: x.Participants
                        .Where(p => IsCloser(p.RoleName))
                        .Select(p => new ParticipantDto(p.UserId, p.FirstName ?? "", p.LastName ?? "", p.SplitPercent ?? 0m))
                        .ToList()
                )
            ).ToList();
        
            return Results.Ok(shaped);
        });
        
        app.MapGet("/org/users/{requesterUserId:int}", async (int requesterUserId, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuth(http, out var authenticatedUserId);
            if (authResult != null) return authResult;

            if (authenticatedUserId != requesterUserId)
                return Results.Forbid();

            var authorityLevel = http.Session.GetInt32("AuthorityLevel") ?? 1;
            if (authorityLevel < 4)
                return Results.Forbid();

            // 1) Find requester and their role/title within their company
            var requester = await db.Users.FindAsync(requesterUserId);
            if (requester is null) return Results.NotFound($"Requester '{requesterUserId}' not found.");
        
            var requesterEmp = await db.Employees
                .Where(er => er.UserId == requesterUserId)
                .Include(er => er.Title)
                .Select(er => new { TitleName = er.Title != null ? er.Title.TitleName : null })
                .FirstOrDefaultAsync();
        
            if (requesterEmp is null) return Results.Forbid();
        
            var role = (requesterEmp.TitleName ?? "").Trim().ToLowerInvariant();
            if (role == "setter" || role == "closer") return Results.Forbid();
        
            // 2) Return the entire org — only the fields you asked for (+ ManagerUserId)
            // CompanyId filter removed - company context comes from schema isolation
            var people = await db.Employees
                .Join(db.Users,
                      er => er.UserId,
                      u => u.UserId,
                      (er, u) => new
                      {
                          u.UserId,
                          u.FirstName,
                          u.LastName,
                          Position = er.Title,      // "position"
                          u.Phone,                  // "phone number"
                          ManagerUserId = (int?)er.ManagerId
                      })
                .ToListAsync();
        
            return Results.Ok(people);
        });
        
        // ========== PROFILE COMMISSION/SALES/GOALS ENDPOINTS ==========

        app.MapGet("/api/profile/{userId:int}/commission-summary", async (
            int userId, HttpContext http, SparkDbContext db, AllocationQueryService allocations,
            DateOnly? periodStart, DateOnly? periodEnd) =>
        {
            var authResult = RequireAuthAndOwnership(http, userId, out _);
            if (authResult != null) return authResult;

            var start = periodStart ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var end = periodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var startDt = start.ToDateTime(TimeOnly.MinValue);
            var endDt = end.ToDateTime(TimeOnly.MaxValue);

            var userAllocations = allocations.GetAllocationsForUser(userId)
                .Where(a => a.CreatedAt >= startDt && a.CreatedAt <= endDt);

            var summary = await userAllocations
                .GroupBy(a => a.Industry)
                .Select(g => new
                {
                    Industry = g.Key,
                    Total = g.Sum(a => a.AllocatedAmount),
                    Pending = g.Where(a => !a.IsApproved).Sum(a => a.AllocatedAmount),
                    Approved = g.Where(a => a.IsApproved && !a.IsPaid).Sum(a => a.AllocatedAmount),
                    Paid = g.Where(a => a.IsPaid).Sum(a => a.AllocatedAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            var overrides = await db.OverrideAllocations
                .Where(oa => oa.UserId == userId && oa.CreatedAt >= startDt && oa.CreatedAt <= endDt)
                .GroupBy(oa => 1)
                .Select(g => new
                {
                    Total = g.Sum(oa => oa.AllocatedAmount),
                    Pending = g.Where(oa => !oa.IsApproved).Sum(oa => oa.AllocatedAmount),
                    Paid = g.Where(oa => oa.IsPaid).Sum(oa => oa.AllocatedAmount)
                })
                .FirstOrDefaultAsync();

            var clawbacks = await db.Clawbacks
                .Where(c => c.UserId == userId && c.CreatedAt >= startDt && c.CreatedAt <= endDt)
                .SumAsync(c => c.ClawbackAmount);

            return ApiResults.Success(new
            {
                Period = new { Start = start, End = end },
                ByIndustry = summary,
                GrandTotal = summary.Sum(s => s.Total),
                Overrides = overrides,
                Clawbacks = clawbacks
            });
        }).RequireAuthorization("Authenticated");

        app.MapGet("/api/profile/{userId:int}/recent-sales", async (
            int userId, HttpContext http, SparkDbContext db, int? count) =>
        {
            var authResult = RequireAuthAndOwnership(http, userId, out _);
            if (authResult != null) return authResult;

            var limit = Math.Clamp(count ?? 10, 1, 50);

            var sales = await db.Sales
                .Where(s => s.SaleParticipants.Any(sp => sp.UserId == userId))
                .OrderByDescending(s => s.SaleDate)
                .Take(limit)
                .Select(s => new
                {
                    s.SaleId,
                    s.SaleDate,
                    s.SaleStatus,
                    s.ContractAmount,
                    ProjectType = s.ProjectType.ProjectTypeName,
                    CustomerName = s.Customer.FirstName + " " + s.Customer.LastName,
                    s.Customer.City,
                    s.Customer.StateCode,
                    s.CreatedAt
                })
                .ToListAsync();

            return ApiResults.Success(sales);
        }).RequireAuthorization("Authenticated");

        app.MapGet("/api/profile/{userId:int}/goal-progress", async (
            int userId, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuthAndOwnership(http, userId, out _);
            if (authResult != null) return authResult;

            // Get employee's team/office/region for org-level goals
            var employee = await db.Employees
                .Include(e => e.Team).ThenInclude(t => t!.Office)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.IsActive);

            var teamId = employee?.TeamId;
            var officeId = employee?.Team?.OfficeId;

            var goals = await db.VGoalProgressSummaries
                .Where(g => g.IsActive == true &&
                    (g.UserId == userId ||
                     (teamId != null && g.TeamId == teamId) ||
                     (officeId != null && g.OfficeId == officeId) ||
                     g.GoalLevel == "COMPANY"))
                .OrderByDescending(g => g.ProgressPercent)
                .ToListAsync();

            return ApiResults.Success(goals);
        }).RequireAuthorization("Authenticated");

        app.MapGet("/sales/{saleId}/details", async (int saleId, HttpContext http, SparkDbContext db) =>
        {
            var authResult = RequireAuth(http, out _);
            if (authResult != null) return authResult;

            var sale = await db.Sales
                .Include(s => s.ProjectType)
                .Include(s => s.Customer)
                .Include(s => s.SolarSale)
                .Include(s => s.RoofingSale)
                .Include(s => s.FiberSale)
                .Include(s => s.ProjectPayouts)
                .Include(s => s.SaleParticipants).ThenInclude(sp => sp.User)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);
            if (sale == null) return Results.NotFound();
            return Results.Ok(sale);
        });
    }
}
