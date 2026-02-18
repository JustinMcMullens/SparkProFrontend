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

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // ================
        // CREATE Endpoints
        // ================

        // NOTE: Companies endpoint removed - companies are now PostgreSQL schemas
        app.MapPost("/companies", () => Results.Json(new { error = "Companies are now managed as database schemas, not table rows" }, statusCode: 501));

        #if false  // Original companies create endpoint - no longer applicable
        app.MapPost("/companies_OLD", async (SparkDbContext db, Company company) =>
        {
            try
            {
                if (company is null)
                    return Results.BadRequest("Company data is required.");
                
                if (string.IsNullOrWhiteSpace(company.Name))
                    return Results.BadRequest("Company name is required.");

                db.Companies.Add(company);
                await db.SaveChangesAsync();
                return Results.Created($"/companies/{company.CompanyId}", company);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error creating company: {ex.Message}");
            }
        });

        #endif  // End of companies create endpoint

        app.MapPost("/login", async (HttpContext http, SparkDbContext db, LoginRequest request) =>
        {
            var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Login");

            try
            {
                if (request is null)
                {
                    logger.LogWarning("Login: request body was null or failed to bind.");
                    return Results.BadRequest("Invalid payload.");
                }

                logger.LogInformation("Login attempt for username '{Username}'", request.Username);

                // Use EF.Functions.ILike for case-insensitive comparison to bypass citext type ambiguity
                var user = await db.Users
                    .Where(u => EF.Functions.ILike(u.Username, request.Username))
                    .FirstOrDefaultAsync();
                    
                if (user == null)
                {
                    logger.LogInformation("Login: user '{Username}' not found.", request.Username);
                    return Results.Unauthorized();
                }

                // Guard against null/empty/invalid hashes
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    logger.LogWarning("Login: user '{Username}' has no password hash.", request.Username);
                    return Results.Unauthorized();
                }

                bool ok;
                try
                {
                    ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Login: BCrypt verify failed for user '{Username}'.", request.Username);
                    return Results.Unauthorized();
                }

                if (!ok)
                {
                    logger.LogInformation("Login: bad password for user '{Username}'.", request.Username);
                    return Results.Unauthorized();
                }

                // Login successful - set session
                http.Session.SetInt32("UserId", user.UserId);
                // CompanyId now comes from schema context, not user table
                http.Session.SetString("Username", user.Username);
                
                // Get user's authority level from employee registry
                int authorityLevel = 1; // Default authority level
                try
                {
                    logger.LogInformation("Login: Fetching employee registry for UserId={UserId}", user.UserId);
                    
                    var employee = await db.Employees
                        .AsNoTracking()
                        .Include(e => e.Title)
                        .Where(e => e.UserId == user.UserId)
                        .Select(e => new { AuthorityLevel = e.Title != null ? (int?)e.Title.TitleLevel : null })
                        .FirstOrDefaultAsync();
                    
                    if (employee != null && employee.AuthorityLevel.HasValue)
                    {
                        authorityLevel = employee.AuthorityLevel.Value;
                        http.Session.SetInt32("AuthorityLevel", authorityLevel);
                        logger.LogInformation("Login: AuthorityLevel set to {AuthorityLevel}", authorityLevel);
                    }
                    else
                    {
                        logger.LogWarning("Login: No employee registry found or no authority level for UserId={UserId}", user.UserId);
                    }
                }
                catch (Exception empEx)
                {
                    logger.LogError(empEx, "Login: Error fetching employee registry for UserId={UserId}", user.UserId);
                    // Don't fail login - just proceed without authority level
                }
                
                await http.Session.CommitAsync(); // Ensure session is saved
                
                logger.LogInformation("Login: Session set for user '{Username}' (UserId: {UserId})", request.Username, user.UserId);

                // Build claims principal for cookie auth
                var roleName = authorityLevel switch
                {
                    >= 5 => "Administrator",
                    >= 4 => "Management",
                    >= 3 => "TeamLead",
                    >= 2 => "SalesRep",
                    _ => "ReadOnly"
                };

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? $"user:{user.UserId}"),
                    new Claim(ClaimTypes.Role, roleName),
                    // CompanyId now comes from schema context - use subdomain/tenant
                    new Claim("CompanyId", TenantContext.GetSubdomain(http) ?? "default"),
                    new Claim("AuthorityLevel", authorityLevel.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                });

                // Fetch companies user has access to
                // NOTE: With schema isolation, this returns the current company from tenant context
                List<dynamic> companies;
                try
                {
                    logger.LogInformation("Login: Fetching companies for UserId={UserId}", user.UserId);
                    
                    var companyName = TenantContext.GetCompanyName(http);
                    var subdomain = TenantContext.GetSubdomain(http);
                    if (!string.IsNullOrEmpty(companyName))
                    {
                        companies = new List<dynamic>
                        {
                            new
                            {
                                id = 1, // Schema isolation means single company context
                                name = companyName,
                                subdomain = subdomain,
                                logoUrl = (string?)null
                            }
                        };
                    }
                    else
                    {
                        companies = new List<dynamic>();
                    }
                    
                    logger.LogInformation("Login: Found {Count} companies for UserId={UserId}", companies.Count, user.UserId);
                }
                catch (Exception compEx)
                {
                    logger.LogError(compEx, "Login: Error fetching companies for UserId={UserId}", user.UserId);
                    companies = new List<dynamic>();
                }

                // Determine last accessed company (default to schema company)
                int lastAccessedCompanyId = 1; // Schema isolation means single company context

                logger.LogInformation("Login: User '{Username}' has access to {Count} companies", request.Username, companies.Count);

                // Fetch user media for profile images
                var loginUserMedia = await db.UserMedia
                    .Where(um => um.UserId == user.UserId)
                    .Select(um => new { um.ProfileImageUrl, um.DashboardBackgroundUrl, um.BannerImageUrl })
                    .FirstOrDefaultAsync();

                return Results.Ok(new
                {
                    userId = user.UserId,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    profileImageUrl = loginUserMedia?.ProfileImageUrl,
                    dashboardBannerUrl = loginUserMedia?.DashboardBackgroundUrl,
                    profileBannerUrl = loginUserMedia?.BannerImageUrl,
                    lastAccessedCompanyId = lastAccessedCompanyId,
                    companies = companies
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login: unhandled exception for '{Username}'.", request?.Username);
                // Avoid leaking details to the client; keep a friendly message
                return Results.Problem("Login failed.");
            }
        });

        // Session validation endpoint
        app.MapGet("/auth/session", async (HttpContext http, SparkDbContext db) =>
        {
            var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("SessionValidation");

            try
            {
                // Check if session has UserId
                var userId = http.Session.GetInt32("UserId");
                if (userId == null)
                {
                    logger.LogInformation("Session validation: No UserId in session");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Session validation: Found UserId {UserId} in session", userId.Value);

                // Fetch user from database
                var user = await db.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    logger.LogWarning("Session validation: User {UserId} not found in database", userId.Value);
                    return Results.Unauthorized();
                }

                // Fetch company from tenant context (schema isolation)
                var sessionCompanyName = TenantContext.GetCompanyName(http);
                var sessionSubdomain = TenantContext.GetSubdomain(http);
                var companies = !string.IsNullOrEmpty(sessionCompanyName)
                    ? new[] { new { id = 1, name = sessionCompanyName, subdomain = sessionSubdomain, logoUrl = (string?)null } }
                    : Array.Empty<object>();

                // Fetch permissions from employee registry
                var employeeRec = await db.Employees
                    .Where(er => er.UserId == userId.Value && er.IsActive)
                    .Include(er => er.Title)
                    .Select(er => new
                    {
                        authorityLevel = er.Title != null ? er.Title.TitleLevel : 2
                    })
                    .FirstOrDefaultAsync();

                var permissions = employeeRec != null
                    ? new[] { new { companyId = 1, authorityLevel = employeeRec.authorityLevel, permissionsJson = (string?)null } }
                    : Array.Empty<object>();

                logger.LogInformation("Session validation: User {UserId} has access to {Count} companies", userId.Value, companies.Length);

                // Fetch user media for profile images
                var sessionUserMedia = await db.UserMedia
                    .Where(um => um.UserId == userId.Value)
                    .Select(um => new { um.ProfileImageUrl, um.DashboardBackgroundUrl, um.BannerImageUrl })
                    .FirstOrDefaultAsync();

                return Results.Ok(new
                {
                    userId = user.UserId,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    profileImageUrl = sessionUserMedia?.ProfileImageUrl,
                    dashboardBannerUrl = sessionUserMedia?.DashboardBackgroundUrl,
                    profileBannerUrl = sessionUserMedia?.BannerImageUrl,
                    companies = companies,
                    permissions = permissions
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Session validation: Unhandled exception");
                return Results.Problem("Session validation failed.");
            }
        });

        // Logout endpoint
        app.MapPost("/auth/logout", async (HttpContext http) =>
        {
            var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Logout");
            
            var userId = http.Session.GetInt32("UserId");
            if (userId != null)
            {
                logger.LogInformation("Logout: Clearing session for UserId {UserId}", userId.Value);
            }
            
            http.Session.Clear();
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Logged out successfully" });
        });

        // Update last accessed company
        app.MapPut("/companies/{companyId:int}/last-accessed", async (HttpContext http, SparkDbContext db, int companyId) =>
        {
            var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UpdateLastAccessed");
            
            // Check session
            var userId = http.Session.GetInt32("UserId");
            if (userId == null)
            {
                logger.LogWarning("UpdateLastAccessed: No session found");
                return Results.Unauthorized();
            }

            // Verify user has an active employee record
            var hasAccess = await db.Employees
                .AnyAsync(er => er.UserId == userId.Value && er.IsActive);

            if (!hasAccess)
            {
                logger.LogWarning("UpdateLastAccessed: User {UserId} does not have an active employee record", userId.Value);
                return Results.Forbid();
            }

            // Store in session
            http.Session.SetInt32("LastAccessedCompanyId", companyId);
            http.Session.SetInt32("CompanyId", companyId);

            // Refresh authority level and re-issue auth cookie claims for new company
            var emp = await db.Employees
                .Include(er => er.Title)
                .FirstOrDefaultAsync(er => er.UserId == userId.Value && er.IsActive);
            var newLevel = emp?.Title?.TitleLevel ?? http.Session.GetInt32("AuthorityLevel") ?? 1;
            http.Session.SetInt32("AuthorityLevel", newLevel);

            var roleName = newLevel switch
            {
                >= 5 => "Administrator",
                >= 4 => "Management",
                >= 3 => "TeamLead",
                >= 2 => "SalesRep",
                _ => "ReadOnly"
            };

            var uid = userId.Value;
            var uname = http.Session.GetString("Username") ?? $"user:{uid}";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, uid.ToString()),
                new Claim(ClaimTypes.Name, uname),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("CompanyId", companyId.ToString()),
                new Claim("AuthorityLevel", newLevel.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            });
            
            logger.LogInformation("UpdateLastAccessed: User {UserId} set last accessed company to {CompanyId}", userId.Value, companyId);
            
            return Results.Ok(new { message = "Last accessed company updated", companyId });
        });
    }
}
