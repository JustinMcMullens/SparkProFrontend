using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;

namespace SparkBackend.Endpoints;

public static class CollaboratorEndpoints
{
    public static void MapCollaboratorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization("Level4Plus");

        // Installers
        group.MapGet("/installers", GetInstallers);
        group.MapPost("/installers", CreateInstaller);
        group.MapPut("/installers/{id:int}", UpdateInstaller);
        group.MapDelete("/installers/{id:int}", DeleteInstaller);
        group.MapGet("/installers/{id:int}/coverage", GetInstallerCoverage);

        // Dealers
        group.MapGet("/dealers", GetDealers);
        group.MapPost("/dealers", CreateDealer);
        group.MapPut("/dealers/{id:int}", UpdateDealer);
        group.MapDelete("/dealers/{id:int}", DeleteDealer);

        // Finance Companies
        group.MapGet("/finance-companies", GetFinanceCompanies);
        group.MapPost("/finance-companies", CreateFinanceCompany);
        group.MapPut("/finance-companies/{id:int}", UpdateFinanceCompany);
        group.MapDelete("/finance-companies/{id:int}", DeleteFinanceCompany);

        // Partners
        group.MapGet("/partners", GetPartners);
        group.MapPost("/partners", CreatePartner);
        group.MapPut("/partners/{id:int}", UpdatePartner);
        group.MapDelete("/partners/{id:int}", DeletePartner);
    }

    // ===== INSTALLERS =====

    private static async Task<IResult> GetInstallers(
        SparkDbContext db, int? page, int? pageSize, bool? isActive)
    {
        var query = db.Installers
            .Include(i => i.CollaboratorCompany)
            .WhereIf(isActive.HasValue, i => i.IsActive == isActive!.Value)
            .OrderBy(i => i.CollaboratorCompany.CompanyName)
            .Select(i => new
            {
                i.InstallerId,
                i.CollaboratorCompanyId,
                CompanyName = i.CollaboratorCompany.CompanyName,
                i.IsPreferred,
                i.IsActive,
                i.Notes,
                i.CreatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> CreateInstaller(
        SparkDbContext db, InstallerCreateRequest request)
    {
        var now = DateTime.UtcNow;

        // Create or find the collaborator company
        var company = await db.CollaboratorCompanies
            .FirstOrDefaultAsync(c => c.CollaboratorCompanyId == request.CollaboratorCompanyId);

        if (company == null)
            return ApiResults.BadRequest("Collaborator company not found");

        var installer = new Installer
        {
            CollaboratorCompanyId = request.CollaboratorCompanyId,
            IsPreferred = request.IsPreferred ?? false,
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Installers.Add(installer);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { installer.InstallerId });
    }

    private static async Task<IResult> UpdateInstaller(int id, SparkDbContext db, InstallerUpdateRequest request)
    {
        var installer = await db.Installers.FindAsync(id);
        if (installer == null) return ApiResults.NotFound("Installer not found");

        if (request.IsPreferred.HasValue) installer.IsPreferred = request.IsPreferred.Value;
        if (request.IsActive.HasValue) installer.IsActive = request.IsActive.Value;
        if (request.Notes != null) installer.Notes = request.Notes;
        installer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ApiResults.Success(new { installer.InstallerId, Updated = true });
    }

    private static async Task<IResult> DeleteInstaller(int id, SparkDbContext db)
    {
        var installer = await db.Installers.FindAsync(id);
        if (installer == null) return ApiResults.NotFound("Installer not found");

        installer.IsActive = false;
        installer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { installer.InstallerId, Deleted = true });
    }

    private static async Task<IResult> GetInstallerCoverage(int id, SparkDbContext db)
    {
        var coverage = await db.InstallerProjectCoverages
            .Where(c => c.CollaboratorCompanyId == id)
            .Select(c => new
            {
                c.CoverageId,
                c.ProjectTypeId,
                c.StateCode,
                c.IsActive,
                c.CoverageStartDate,
                c.CoverageEndDate
            })
            .ToListAsync();

        return ApiResults.Success(coverage);
    }

    // ===== DEALERS =====

    private static async Task<IResult> GetDealers(
        SparkDbContext db, int? page, int? pageSize, bool? isActive)
    {
        // Dealers are CollaboratorCompanies with a specific type
        var query = db.CollaboratorCompanies
            .WhereIf(isActive.HasValue, c => c.IsActive == isActive!.Value)
            .Where(c => c.CollaboratorCompanyType.TypeName == "Dealer")
            .OrderBy(c => c.CompanyName)
            .Select(c => new
            {
                c.CollaboratorCompanyId,
                c.CompanyName,
                c.IsActive,
                c.CreatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> CreateDealer(SparkDbContext db, CollaboratorCreateRequest request)
    {
        var dealerType = await db.CollaboratorCompanyTypes
            .FirstOrDefaultAsync(t => t.TypeName == "Dealer");
        if (dealerType == null) return ApiResults.BadRequest("Dealer type not configured");

        var company = new CollaboratorCompany
        {
            CollaboratorCompanyTypeId = dealerType.CollaboratorCompanyTypeId,
            CompanyName = request.CompanyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.CollaboratorCompanies.Add(company);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId });
    }

    private static async Task<IResult> UpdateDealer(int id, SparkDbContext db, CollaboratorUpdateRequest request)
    {
        var company = await db.CollaboratorCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Dealer not found");

        if (request.CompanyName != null) company.CompanyName = request.CompanyName;
        if (request.IsActive.HasValue) company.IsActive = request.IsActive.Value;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId, Updated = true });
    }

    private static async Task<IResult> DeleteDealer(int id, SparkDbContext db)
    {
        var company = await db.CollaboratorCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Dealer not found");
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId, Deleted = true });
    }

    // ===== FINANCE COMPANIES =====

    private static async Task<IResult> GetFinanceCompanies(
        SparkDbContext db, int? page, int? pageSize, bool? isActive)
    {
        var query = db.FinanceCompanies
            .WhereIf(isActive.HasValue, c => c.IsActive == isActive!.Value)
            .OrderBy(c => c.CompanyName)
            .Select(c => new
            {
                c.FinanceCompanyId,
                c.CompanyName,
                c.IsActive,
                c.CreatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> CreateFinanceCompany(SparkDbContext db, FinanceCompanyCreateRequest request)
    {
        var company = new FinanceCompany
        {
            CompanyName = request.CompanyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.FinanceCompanies.Add(company);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.FinanceCompanyId });
    }

    private static async Task<IResult> UpdateFinanceCompany(int id, SparkDbContext db, CollaboratorUpdateRequest request)
    {
        var company = await db.FinanceCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Finance company not found");
        if (request.CompanyName != null) company.CompanyName = request.CompanyName;
        if (request.IsActive.HasValue) company.IsActive = request.IsActive.Value;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.FinanceCompanyId, Updated = true });
    }

    private static async Task<IResult> DeleteFinanceCompany(int id, SparkDbContext db)
    {
        var company = await db.FinanceCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Finance company not found");
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.FinanceCompanyId, Deleted = true });
    }

    // ===== PARTNERS =====

    private static async Task<IResult> GetPartners(
        SparkDbContext db, int? page, int? pageSize, bool? isActive)
    {
        var query = db.CollaboratorCompanies
            .WhereIf(isActive.HasValue, c => c.IsActive == isActive!.Value)
            .Where(c => c.CollaboratorCompanyType.TypeName == "Partner")
            .OrderBy(c => c.CompanyName)
            .Select(c => new
            {
                c.CollaboratorCompanyId,
                c.CompanyName,
                c.IsActive,
                c.CreatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> CreatePartner(SparkDbContext db, CollaboratorCreateRequest request)
    {
        var partnerType = await db.CollaboratorCompanyTypes
            .FirstOrDefaultAsync(t => t.TypeName == "Partner");
        if (partnerType == null) return ApiResults.BadRequest("Partner type not configured");

        var company = new CollaboratorCompany
        {
            CollaboratorCompanyTypeId = partnerType.CollaboratorCompanyTypeId,
            CompanyName = request.CompanyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.CollaboratorCompanies.Add(company);
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId });
    }

    private static async Task<IResult> UpdatePartner(int id, SparkDbContext db, CollaboratorUpdateRequest request)
    {
        var company = await db.CollaboratorCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Partner not found");
        if (request.CompanyName != null) company.CompanyName = request.CompanyName;
        if (request.IsActive.HasValue) company.IsActive = request.IsActive.Value;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId, Updated = true });
    }

    private static async Task<IResult> DeletePartner(int id, SparkDbContext db)
    {
        var company = await db.CollaboratorCompanies.FindAsync(id);
        if (company == null) return ApiResults.NotFound("Partner not found");
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ApiResults.Success(new { company.CollaboratorCompanyId, Deleted = true });
    }
}

// DTOs for collaborator endpoints
public record InstallerCreateRequest(int CollaboratorCompanyId, bool? IsPreferred, string? Notes);
public record InstallerUpdateRequest(bool? IsPreferred, bool? IsActive, string? Notes);
public record CollaboratorCreateRequest(string CompanyName);
public record CollaboratorUpdateRequest(string? CompanyName, bool? IsActive);
public record FinanceCompanyCreateRequest(string CompanyName);
