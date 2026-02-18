using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Middleware;
using SparkBackend.Services;

namespace SparkBackend.Endpoints;

public static class SalesEndpoints
{
    public static void MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetSales);
        group.MapGet("/{saleId:int}", GetSaleDetail);
        group.MapPost("/{saleId:int}/cancel", CancelSale);
        group.MapGet("/{saleId:int}/allocations", GetSaleAllocations);
        group.MapGet("/{saleId:int}/notes", GetSaleNotes);
    }

    private static async Task<IResult> GetSales(
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        int? page, int? pageSize,
        string? status, DateOnly? dateFrom, DateOnly? dateTo,
        int? projectTypeId, int? userId, int? installerId,
        int? customerId, int? generationTypeId,
        decimal? contractAmountMin, decimal? contractAmountMax,
        string? sortBy, string? sortDir)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var query = db.Sales
            .Include(s => s.Customer)
            .Include(s => s.ProjectType)
            .Include(s => s.SaleParticipants).ThenInclude(sp => sp.User)
            .Include(s => s.SaleParticipants).ThenInclude(sp => sp.Role)
            .AsQueryable();

        // Authority scoping: < Level 4 only sees sales they participate in
        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            query = query.Where(s => s.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)));
        }

        // Filters
        query = query
            .WhereIf(!string.IsNullOrEmpty(status), s => s.SaleStatus == status)
            .WhereIf(dateFrom.HasValue, s => s.SaleDate >= dateFrom!.Value)
            .WhereIf(dateTo.HasValue, s => s.SaleDate <= dateTo!.Value)
            .WhereIf(projectTypeId.HasValue, s => s.ProjectTypeId == projectTypeId!.Value)
            .WhereIf(userId.HasValue, s => s.SaleParticipants.Any(sp => sp.UserId == userId!.Value))
            .WhereIf(customerId.HasValue, s => s.CustomerId == customerId!.Value)
            .WhereIf(generationTypeId.HasValue, s => s.GenerationTypeId == generationTypeId!.Value)
            .WhereIf(contractAmountMin.HasValue, s => s.ContractAmount >= contractAmountMin!.Value)
            .WhereIf(contractAmountMax.HasValue, s => s.ContractAmount <= contractAmountMax!.Value);

        // Installer filter: check across all industry sale types
        if (installerId.HasValue)
        {
            query = query.Where(s =>
                (s.SolarSale != null && s.SolarSale.InstallerId == installerId.Value) ||
                (s.RoofingSale != null && s.RoofingSale.PartnerId == installerId.Value));
        }

        // Sorting
        query = (sortBy?.ToLower()) switch
        {
            "date" => sortDir == "asc" ? query.OrderBy(s => s.SaleDate) : query.OrderByDescending(s => s.SaleDate),
            "amount" => sortDir == "asc" ? query.OrderBy(s => s.ContractAmount) : query.OrderByDescending(s => s.ContractAmount),
            "status" => sortDir == "asc" ? query.OrderBy(s => s.SaleStatus) : query.OrderByDescending(s => s.SaleStatus),
            "customer" => sortDir == "asc" ? query.OrderBy(s => s.Customer.LastName) : query.OrderByDescending(s => s.Customer.LastName),
            _ => query.OrderByDescending(s => s.SaleDate)
        };

        // Project to DTO before pagination to avoid cycle issues
        var projected = query.Select(s => new
        {
            s.SaleId,
            s.SaleDate,
            s.SaleStatus,
            s.ContractAmount,
            s.IsActive,
            ProjectType = s.ProjectType.ProjectTypeName,
            GenerationType = s.GenerationType != null ? s.GenerationType.GenerationTypeName : null,
            Customer = new
            {
                s.Customer.CustomerId,
                s.Customer.FirstName,
                s.Customer.LastName,
                s.Customer.City,
                s.Customer.StateCode
            },
            Participants = s.SaleParticipants.Select(sp => new
            {
                sp.UserId,
                sp.User.User.FirstName,
                sp.User.User.LastName,
                Role = sp.Role.RoleName,
                sp.ParticipationType,
                sp.SplitPercent,
                sp.IsPrimary
            }),
            s.CreatedAt
        });

        var result = await projected.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetSaleDetail(
        int saleId,
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions,
        AllocationQueryService allocations)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var sale = await db.Sales
            .Include(s => s.Customer)
            .Include(s => s.ProjectType)
            .Include(s => s.GenerationType)
            .Include(s => s.SaleParticipants).ThenInclude(sp => sp.User).ThenInclude(e => e.User)
            .Include(s => s.SaleParticipants).ThenInclude(sp => sp.Role)
            .Include(s => s.SaleTotal)
            .Include(s => s.ProjectPayouts)
            .Include(s => s.OverrideAllocations).ThenInclude(oa => oa.User).ThenInclude(e => e.User)
            .Include(s => s.Clawbacks)
            .Include(s => s.SolarSale)
            .Include(s => s.PestSale)
            .Include(s => s.RoofingSale)
            .Include(s => s.FiberSale)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
            return ApiResults.NotFound("Sale not found");

        // Authority check: ensure user can see this sale
        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            if (!sale.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)))
                return ApiResults.Forbidden("You do not have access to this sale");
        }

        // Get allocations via unified service
        var saleAllocations = await allocations.GetAllocationsForSale(saleId).ToListAsync();

        // Determine which industry extension to include
        object? industryDetail = sale.SolarSale != null ? new
        {
            Industry = "Solar",
            sale.SolarSale.SystemSizeKw,
            sale.SolarSale.PurchaseOrLease,
            sale.SolarSale.InstallDate,
            sale.SolarSale.SignedDate,
            sale.SolarSale.CompletedDate,
            sale.SolarSale.SystemSoldValue,
            sale.SolarSale.PpwSold,
            sale.SolarSale.PpwGross,
            sale.SolarSale.PpwNet,
            sale.SolarSale.AdderBreakdown,
            sale.SolarSale.AdderTotal,
            sale.SolarSale.Product,
            sale.SolarSale.FinanceRate,
            sale.SolarSale.CommissionPaymentBasis
        } : sale.PestSale != null ? new
        {
            Industry = "Pest",
            sale.PestSale.ServiceType,
            sale.PestSale.ServiceFrequency,
            sale.PestSale.SignedDate,
            sale.PestSale.CompletedDate,
            sale.PestSale.ContractStartDate,
            sale.PestSale.ContractEndDate,
            sale.PestSale.ContractLengthMonths,
            sale.PestSale.InitialServicePrice,
            sale.PestSale.RecurringPrice,
            sale.PestSale.ContractTotalValue,
            sale.PestSale.CommissionPaymentBasis
        } as object : sale.RoofingSale != null ? new
        {
            Industry = "Roofing",
            sale.RoofingSale.InstallDate,
            sale.RoofingSale.CompletionDate,
            sale.RoofingSale.SignedDate,
            sale.RoofingSale.ProjectValue,
            sale.RoofingSale.FrontendReceivedAmount,
            sale.RoofingSale.BackendReceivedAmount,
            sale.RoofingSale.FinanceRate,
            sale.RoofingSale.CommissionPaymentBasis
        } as object : sale.FiberSale != null ? new
        {
            Industry = "Fiber",
            sale.FiberSale.InstallDate,
            sale.FiberSale.SignedDate,
            sale.FiberSale.CompletedDate,
            sale.FiberSale.Location,
            sale.FiberSale.Isp,
            sale.FiberSale.FiberPlan,
            sale.FiberSale.CommissionPaymentBasis
        } as object : null;

        var result = new
        {
            sale.SaleId,
            sale.SaleDate,
            sale.SaleStatus,
            sale.ContractAmount,
            sale.IsActive,
            sale.CancelledAt,
            sale.CancellationReason,
            ProjectType = new { sale.ProjectType.ProjectTypeId, sale.ProjectType.ProjectTypeName },
            GenerationType = sale.GenerationType != null ? new { sale.GenerationType.GenerationTypeId, sale.GenerationType.GenerationTypeName } : null,
            Customer = new
            {
                sale.Customer.CustomerId,
                sale.Customer.FirstName,
                sale.Customer.LastName,
                sale.Customer.Email,
                sale.Customer.Phone,
                sale.Customer.AddressLine1,
                sale.Customer.AddressLine2,
                sale.Customer.City,
                sale.Customer.StateCode,
                sale.Customer.PostalCode
            },
            Participants = sale.SaleParticipants.Select(sp => new
            {
                sp.UserId,
                sp.User.User.FirstName,
                sp.User.User.LastName,
                Role = sp.Role.RoleName,
                sp.ParticipationType,
                sp.SplitPercent,
                sp.IsPrimary
            }),
            SaleTotal = sale.SaleTotal != null ? new
            {
                sale.SaleTotal.Received,
                sale.SaleTotal.Payroll,
                sale.SaleTotal.Expenses,
                sale.SaleTotal.SaleProfit
            } : null,
            ProjectPayouts = sale.ProjectPayouts.Select(pp => new
            {
                pp.ProjectPayoutId,
                pp.MilestoneNumber,
                pp.MilestoneName,
                pp.PayoutAmount,
                pp.PayoutDate,
                pp.IsPaid,
                pp.PaidAt
            }),
            Allocations = saleAllocations,
            Overrides = sale.OverrideAllocations.Select(oa => new
            {
                oa.AllocationId,
                oa.UserId,
                oa.User.User.FirstName,
                oa.User.User.LastName,
                oa.OverrideLevel,
                oa.AllocatedAmount,
                oa.IsApproved,
                oa.IsPaid
            }),
            Clawbacks = sale.Clawbacks.Select(c => new
            {
                c.ClawbackId,
                c.UserId,
                c.ClawbackAmount,
                c.ClawbackReason,
                c.ClawbackDate,
                c.IsProcessed
            }),
            IndustryDetail = industryDetail,
            sale.CreatedAt,
            sale.UpdatedAt
        };

        return ApiResults.Success(result);
    }

    private static async Task<IResult> CancelSale(
        int saleId,
        CancelSaleRequest request,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var sale = await db.Sales.FindAsync(saleId);
        if (sale == null)
            return ApiResults.NotFound("Sale not found");

        if (sale.SaleStatus == "CANCELLED")
            return ApiResults.BadRequest("Sale is already cancelled");

        sale.SaleStatus = "CANCELLED";
        sale.CancelledAt = DateTime.UtcNow;
        sale.CancelledBy = currentUserId;
        sale.CancellationReason = request.Reason;
        sale.UpdatedAt = DateTime.UtcNow;
        sale.UpdatedBy = currentUserId;

        await db.SaveChangesAsync();

        return ApiResults.Success(new
        {
            sale.SaleId,
            sale.SaleStatus,
            sale.CancelledAt,
            sale.CancellationReason
        });
    }

    private static async Task<IResult> GetSaleAllocations(
        int saleId,
        HttpContext http,
        SparkDbContext db,
        AllocationQueryService allocations,
        PermissionsService permissions)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        // Verify sale exists and user has access
        var sale = await db.Sales
            .Include(s => s.SaleParticipants)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
            return ApiResults.NotFound("Sale not found");

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            if (!sale.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)))
                return ApiResults.Forbidden("You do not have access to this sale");
        }

        var saleAllocations = await allocations.GetAllocationsForSale(saleId).ToListAsync();

        var overrides = await db.OverrideAllocations
            .Where(oa => oa.SaleId == saleId)
            .Select(oa => new
            {
                oa.AllocationId,
                oa.UserId,
                oa.OverrideLevel,
                oa.AllocatedAmount,
                oa.IsApproved,
                oa.IsPaid,
                oa.CreatedAt
            })
            .ToListAsync();

        return ApiResults.Success(new { Allocations = saleAllocations, Overrides = overrides });
    }

    private static async Task<IResult> GetSaleNotes(
        int saleId,
        HttpContext http,
        SparkDbContext db,
        PermissionsService permissions)
    {
        var authResult = EndpointAuthHelpers.RequireAuth(http, out var currentUserId);
        if (authResult != null) return authResult;

        var companyId = http.Session.GetInt32("CompanyId") ?? 0;
        var authorityLevel = EndpointAuthHelpers.GetAuthorityLevel(http);

        var sale = await db.Sales
            .Include(s => s.SaleParticipants)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
            return ApiResults.NotFound("Sale not found");

        if (authorityLevel < 4)
        {
            var accessibleIds = await permissions.GetAccessibleUserIdsAsync(currentUserId, companyId);
            if (!sale.SaleParticipants.Any(sp => accessibleIds.Contains(sp.UserId)))
                return ApiResults.Forbidden("You do not have access to this sale");
        }

        var customerNotes = await db.SaleCustomerNotes
            .Where(n => n.SaleId == saleId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.NoteId,
                n.UserId,
                n.User.User.FirstName,
                n.User.User.LastName,
                n.NoteText,
                n.ContactMethod,
                n.ContactDate,
                n.FollowUpDate,
                n.CreatedAt
            })
            .ToListAsync();

        var projectNotes = await db.SaleProjectNotes
            .Where(n => n.SaleId == saleId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.NoteId,
                n.UserId,
                n.User.User.FirstName,
                n.User.User.LastName,
                n.NoteText,
                n.CreatedAt
            })
            .ToListAsync();

        return ApiResults.Success(new { CustomerNotes = customerNotes, ProjectNotes = projectNotes });
    }
}
