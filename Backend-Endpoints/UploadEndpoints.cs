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

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        // POST /upload-file - Basic file upload endpoint
        app.MapPost("/upload-file", async (HttpContext http, IWebHostEnvironment env) =>
        {
            var authUserId = GetAuthenticatedUserId(http);
            if (authUserId == null)
                return Results.Unauthorized();
        
            try
            {
                if (!http.Request.HasFormContentType)
                    return Results.BadRequest("Invalid content type. Expected multipart/form-data.");
        
                var form = await http.Request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
        
                if (file == null || file.Length == 0)
                    return Results.BadRequest("No file uploaded.");
        
                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                    return Results.BadRequest("File size exceeds 10MB limit.");
        
                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(env.ContentRootPath, "uploads", "payouts");
                Directory.CreateDirectory(uploadsDir);
        
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
        
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
        
                return Results.Ok(new
                {
                    Message = "File uploaded successfully",
                    FileName = fileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    OriginalName = file.FileName
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error uploading file: {ex.Message}");
            }
        });
        
        // POST /api/upload - Upload and parse payout file (PDF/CSV)
        app.MapPost("/api/upload", async (HttpContext http, IWebHostEnvironment env, SparkDbContext db) =>
        {
            var authUserId = GetAuthenticatedUserId(http);
            if (authUserId == null)
                return Results.Unauthorized();
        
            try
            {
                var user = await db.Users.FindAsync(authUserId.Value);
                var emp = user == null ? null : await db.Employees
                    .Include(e => e.Title)
                    .FirstOrDefaultAsync(er => er.UserId == user.UserId);
                if (user == null || (emp?.Title?.TitleLevel ?? 0) < 4)
                    return Results.Forbid();
        
                if (!http.Request.HasFormContentType)
                    return Results.BadRequest("Invalid content type. Expected multipart/form-data.");
        
                var form = await http.Request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
        
                if (file == null || file.Length == 0)
                    return Results.BadRequest("No file uploaded.");
        
                // For now, return a stub response indicating parsing is not yet implemented
                // In a real implementation, you would use a PDF parsing library or CSV parser here
                var stubPayouts = new[]
                {
                    new
                    {
                        SaleId = (int?)null,
                        Type = "Commission",
                        ActualDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        Description = "Parsed from file - requires manual verification",
                        Error = "Automatic parsing not yet implemented. Please enter data manually."
                    }
                };
        
                return Results.Ok(stubPayouts);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error parsing file: {ex.Message}");
            }
        });
        
        // POST /api/upload/save - Save parsed payout data
        app.MapPost("/api/upload/save", async (HttpContext http, SparkDbContext db, List<PayoutUploadEntry> entries) =>
        {
            var authUserId = GetAuthenticatedUserId(http);
            if (authUserId == null)
                return Results.Unauthorized();
        
            try
            {
                var user = await db.Users.FindAsync(authUserId.Value);
                var emp = user == null ? null : await db.Employees
                    .Include(e => e.Title)
                    .FirstOrDefaultAsync(er => er.UserId == user.UserId);
                if (user == null || (emp?.Title?.TitleLevel ?? 0) < 4)
                    return Results.Forbid();
        
                if (entries == null || entries.Count == 0)
                    return Results.BadRequest("No payout entries provided.");
        
                var savedCount = 0;
                var errors = new List<string>();
        
                foreach (var entry in entries)
                {
                    if (!entry.SaleId.HasValue || entry.SaleId.Value <= 0)
                    {
                        errors.Add($"Invalid SaleId for entry: {entry.Description}");
                        continue;
                    }
        
                    // Verify sale exists
                    var sale = await db.Sales.FindAsync(entry.SaleId.Value);
                    if (sale == null)
                    {
                        errors.Add($"Sale {entry.SaleId.Value} not found");
                        continue;
                    }
        
                    // Create a project payout or commission allocation based on type
                    if (entry.Type?.ToLower().Contains("milestone") == true || entry.Type?.ToLower().Contains("mp") == true)
                    {
                        // This would be a ProjectPayout - stub for now
                        errors.Add($"ProjectPayout creation from upload not yet implemented for Sale {entry.SaleId.Value}");
                    }
                    else
                    {
                        // This would be a CommissionAllocation - stub for now
                        errors.Add($"CommissionAllocation creation from upload not yet implemented for Sale {entry.SaleId.Value}");
                    }
                }
        
                return Results.Ok(new
                {
                    Message = $"Processed {entries.Count} entries. {savedCount} saved successfully.",
                    SavedCount = savedCount,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error saving payout entries: {ex.Message}");
            }
        });
    }
}
