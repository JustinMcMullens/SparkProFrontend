using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Middleware;
using System.Text.Json;

namespace SparkBackend.Endpoints;

// TODO: This endpoint needs to be rewritten for the new schema architecture
// Key changes:
// - Installers are now linked to CollaboratorCompany via collaborator_company_id
// - Commission defaults are now in SolarCommissionCompanySettings
// - InstallerName comes from CollaboratorCompany.CompanyName
// - States/coverage handled by InstallerProjectCoverage

public static class SolarInstallerEndpoints
{
    public static void MapSolarInstallerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/solar/installers")
            .WithTags("Solar Installers");

        // GET /api/solar/installers - Get all installers
        group.MapGet("/", async (HttpContext http, SparkDbContext db) =>
        {
            try
            {
                var installers = await db.Installers
                    .Include(i => i.CollaboratorCompany)
                    .Where(i => i.IsActive)
                    .Select(i => new
                    {
                        InstallerId = i.InstallerId,
                        InstallerName = i.CollaboratorCompany.CompanyName,
                        IsPreferred = i.IsPreferred,
                        Notes = i.Notes
                    })
                    .ToListAsync();

                return Results.Ok(new { installers });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { error = $"Error fetching installers: {ex.Message}" },
                    statusCode: 500
                );
            }
        })
        .WithName("GetSolarInstallers")
        .WithDescription("Get all solar installers");

        // GET /api/solar/installers/{id}
        group.MapGet("/{id:int}", async (int id, SparkDbContext db) =>
        {
            var installer = await db.Installers
                .Include(i => i.CollaboratorCompany)
                .Where(i => i.InstallerId == id)
                .Select(i => new
                {
                    InstallerId = i.InstallerId,
                    InstallerName = i.CollaboratorCompany.CompanyName,
                    IsPreferred = i.IsPreferred,
                    IsActive = i.IsActive,
                    Notes = i.Notes
                })
                .FirstOrDefaultAsync();

            if (installer == null)
                return Results.NotFound();

            return Results.Ok(installer);
        })
        .WithName("GetSolarInstallerById")
        .WithDescription("Get a specific solar installer");

        // Other endpoints return 501 Not Implemented for now
        group.MapPost("/", () => Results.Json(new { error = "Endpoint needs rewrite for new schema" }, statusCode: 501));
        group.MapPut("/{id:int}", (int id) => Results.Json(new { error = "Endpoint needs rewrite for new schema" }, statusCode: 501));
        group.MapDelete("/{id:int}", (int id) => Results.Json(new { error = "Endpoint needs rewrite for new schema" }, statusCode: 501));
        group.MapGet("/{id:int}/commission-settings", (int id) => Results.Json(new { error = "Endpoint needs rewrite for new schema" }, statusCode: 501));
        group.MapPut("/{id:int}/commission-settings", (int id) => Results.Json(new { error = "Endpoint needs rewrite for new schema" }, statusCode: 501));
    }
}
