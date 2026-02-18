using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;
using SparkBackend.Helpers;
using SparkBackend.Models;

namespace SparkBackend.Endpoints;

public static class RateEndpoints
{
    public static void MapRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rates")
            .RequireAuthorization("Authenticated");

        group.MapGet("/{industry}", GetRates);
        group.MapGet("/{industry}/user/{userId:int}", GetRatesForUser);
        group.MapPost("/{industry}", CreateRate);
        group.MapPut("/{industry}/{rateId:int}", UpdateRate);
        group.MapDelete("/{industry}/{rateId:int}", DeleteRate);
    }

    private static async Task<IResult> GetRates(
        string industry,
        HttpContext http,
        SparkDbContext db,
        int? page, int? pageSize,
        int? userId, int? roleId, string? stateCode, bool? isActive)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out _);
        if (authResult != null) return authResult;

        return industry.ToLower() switch
        {
            "solar" => await GetRatesGeneric(db.SolarUserCommissionRates, page, pageSize, userId, roleId, stateCode, isActive),
            "pest" => await GetRatesGeneric(db.PestUserCommissionRates, page, pageSize, userId, roleId, stateCode, isActive),
            "roofing" => await GetRatesGeneric(db.RoofingUserCommissionRates, page, pageSize, userId, roleId, stateCode, isActive),
            "fiber" => await GetRatesGeneric(db.FiberUserCommissionRates, page, pageSize, userId, roleId, stateCode, isActive),
            _ => ApiResults.BadRequest("Invalid industry. Use: solar, pest, roofing, fiber")
        };
    }

    private static async Task<IResult> GetRatesGeneric(
        IQueryable<SolarUserCommissionRate> rates, int? page, int? pageSize,
        int? userId, int? roleId, string? stateCode, bool? isActive)
    {
        var query = rates
            .WhereIf(userId.HasValue, r => r.UserId == userId!.Value)
            .WhereIf(roleId.HasValue, r => r.RoleId == roleId!.Value)
            .WhereIf(!string.IsNullOrEmpty(stateCode), r => r.StateCode == stateCode)
            .WhereIf(isActive.HasValue, r => r.IsActive == isActive!.Value)
            .OrderByDescending(r => r.EffectiveStartDate)
            .Select(r => new
            {
                r.RateId, r.UserId, r.RoleId, r.InstallerId, r.StateCode,
                r.PercentMp1, r.FlatMp1, r.PercentMp2, r.FlatMp2,
                r.IsActive, r.EffectiveStartDate, r.EffectiveEndDate,
                r.CreatedAt, r.UpdatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetRatesGeneric(
        IQueryable<PestUserCommissionRate> rates, int? page, int? pageSize,
        int? userId, int? roleId, string? stateCode, bool? isActive)
    {
        var query = rates
            .WhereIf(userId.HasValue, r => r.UserId == userId!.Value)
            .WhereIf(roleId.HasValue, r => r.RoleId == roleId!.Value)
            .WhereIf(!string.IsNullOrEmpty(stateCode), r => r.StateCode == stateCode)
            .WhereIf(isActive.HasValue, r => r.IsActive == isActive!.Value)
            .OrderByDescending(r => r.EffectiveStartDate)
            .Select(r => new
            {
                r.RateId, r.UserId, r.RoleId, r.InstallerId, r.StateCode,
                r.PercentMp1, r.FlatMp1, r.PercentMp2, r.FlatMp2,
                r.IsActive, r.EffectiveStartDate, r.EffectiveEndDate,
                r.CreatedAt, r.UpdatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetRatesGeneric(
        IQueryable<RoofingUserCommissionRate> rates, int? page, int? pageSize,
        int? userId, int? roleId, string? stateCode, bool? isActive)
    {
        var query = rates
            .WhereIf(userId.HasValue, r => r.UserId == userId!.Value)
            .WhereIf(roleId.HasValue, r => r.RoleId == roleId!.Value)
            .WhereIf(!string.IsNullOrEmpty(stateCode), r => r.StateCode == stateCode)
            .WhereIf(isActive.HasValue, r => r.IsActive == isActive!.Value)
            .OrderByDescending(r => r.EffectiveStartDate)
            .Select(r => new
            {
                r.RateId, r.UserId, r.RoleId, r.InstallerId, r.StateCode,
                r.PercentMp1, r.FlatMp1, r.PercentMp2, r.FlatMp2,
                r.IsActive, r.EffectiveStartDate, r.EffectiveEndDate,
                r.CreatedAt, r.UpdatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetRatesGeneric(
        IQueryable<FiberUserCommissionRate> rates, int? page, int? pageSize,
        int? userId, int? roleId, string? stateCode, bool? isActive)
    {
        var query = rates
            .WhereIf(userId.HasValue, r => r.UserId == userId!.Value)
            .WhereIf(roleId.HasValue, r => r.RoleId == roleId!.Value)
            .WhereIf(!string.IsNullOrEmpty(stateCode), r => r.StateCode == stateCode)
            .WhereIf(isActive.HasValue, r => r.IsActive == isActive!.Value)
            .OrderByDescending(r => r.EffectiveStartDate)
            .Select(r => new
            {
                r.RateId, r.UserId, r.RoleId, r.InstallerId, r.StateCode,
                r.PercentMp1, r.FlatMp1, r.PercentMp2, r.FlatMp2,
                r.IsActive, r.EffectiveStartDate, r.EffectiveEndDate,
                r.CreatedAt, r.UpdatedAt
            });

        var result = await query.PaginateAsync(page, pageSize);
        return ApiResults.Paged(result.Items, page ?? 1, pageSize ?? 25, result.TotalCount);
    }

    private static async Task<IResult> GetRatesForUser(
        string industry,
        int userId,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 3, out _);
        if (authResult != null) return authResult;

        return industry.ToLower() switch
        {
            "solar" => await GetRatesGeneric(db.SolarUserCommissionRates.Where(r => r.UserId == userId), null, 100, null, null, null, null),
            "pest" => await GetRatesGeneric(db.PestUserCommissionRates.Where(r => r.UserId == userId), null, 100, null, null, null, null),
            "roofing" => await GetRatesGeneric(db.RoofingUserCommissionRates.Where(r => r.UserId == userId), null, 100, null, null, null, null),
            "fiber" => await GetRatesGeneric(db.FiberUserCommissionRates.Where(r => r.UserId == userId), null, 100, null, null, null, null),
            _ => ApiResults.BadRequest("Invalid industry. Use: solar, pest, roofing, fiber")
        };
    }

    private static async Task<IResult> CreateRate(
        string industry,
        HttpContext http,
        SparkDbContext db,
        RateCreateRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;

        switch (industry.ToLower())
        {
            case "solar":
                var solarRate = new SolarUserCommissionRate
                {
                    UserId = request.UserId, RoleId = request.RoleId, InstallerId = request.InstallerId,
                    StateCode = request.StateCode, PercentMp1 = request.PercentMp1, FlatMp1 = request.FlatMp1,
                    PercentMp2 = request.PercentMp2, FlatMp2 = request.FlatMp2, IsActive = true,
                    EffectiveStartDate = request.EffectiveStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    EffectiveEndDate = request.EffectiveEndDate,
                    CreatedAt = now, UpdatedAt = now, CreatedBy = currentUserId
                };
                db.SolarUserCommissionRates.Add(solarRate);
                await db.SaveChangesAsync();
                return ApiResults.Success(new { solarRate.RateId, Industry = "solar" });

            case "pest":
                var pestRate = new PestUserCommissionRate
                {
                    UserId = request.UserId, RoleId = request.RoleId, InstallerId = request.InstallerId,
                    StateCode = request.StateCode, PercentMp1 = request.PercentMp1, FlatMp1 = request.FlatMp1,
                    PercentMp2 = request.PercentMp2, FlatMp2 = request.FlatMp2, IsActive = true,
                    EffectiveStartDate = request.EffectiveStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    EffectiveEndDate = request.EffectiveEndDate,
                    CreatedAt = now, UpdatedAt = now, CreatedBy = currentUserId
                };
                db.PestUserCommissionRates.Add(pestRate);
                await db.SaveChangesAsync();
                return ApiResults.Success(new { pestRate.RateId, Industry = "pest" });

            case "roofing":
                var roofingRate = new RoofingUserCommissionRate
                {
                    UserId = request.UserId, RoleId = request.RoleId, InstallerId = request.InstallerId,
                    StateCode = request.StateCode, PercentMp1 = request.PercentMp1, FlatMp1 = request.FlatMp1,
                    PercentMp2 = request.PercentMp2, FlatMp2 = request.FlatMp2, IsActive = true,
                    EffectiveStartDate = request.EffectiveStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    EffectiveEndDate = request.EffectiveEndDate,
                    CreatedAt = now, UpdatedAt = now, CreatedBy = currentUserId
                };
                db.RoofingUserCommissionRates.Add(roofingRate);
                await db.SaveChangesAsync();
                return ApiResults.Success(new { roofingRate.RateId, Industry = "roofing" });

            case "fiber":
                var fiberRate = new FiberUserCommissionRate
                {
                    UserId = request.UserId, RoleId = request.RoleId, InstallerId = request.InstallerId,
                    StateCode = request.StateCode, PercentMp1 = request.PercentMp1, FlatMp1 = request.FlatMp1,
                    PercentMp2 = request.PercentMp2, FlatMp2 = request.FlatMp2, IsActive = true,
                    EffectiveStartDate = request.EffectiveStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    EffectiveEndDate = request.EffectiveEndDate,
                    CreatedAt = now, UpdatedAt = now, CreatedBy = currentUserId
                };
                db.FiberUserCommissionRates.Add(fiberRate);
                await db.SaveChangesAsync();
                return ApiResults.Success(new { fiberRate.RateId, Industry = "fiber" });

            default:
                return ApiResults.BadRequest("Invalid industry. Use: solar, pest, roofing, fiber");
        }
    }

    private static async Task<IResult> UpdateRate(
        string industry,
        int rateId,
        HttpContext http,
        SparkDbContext db,
        RateUpdateRequest request)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;

        switch (industry.ToLower())
        {
            case "solar":
                var solar = await db.SolarUserCommissionRates.FindAsync(rateId);
                if (solar == null) return ApiResults.NotFound("Rate not found");
                ApplyUpdate(solar, request, currentUserId, now);
                break;
            case "pest":
                var pest = await db.PestUserCommissionRates.FindAsync(rateId);
                if (pest == null) return ApiResults.NotFound("Rate not found");
                ApplyUpdate(pest, request, currentUserId, now);
                break;
            case "roofing":
                var roofing = await db.RoofingUserCommissionRates.FindAsync(rateId);
                if (roofing == null) return ApiResults.NotFound("Rate not found");
                ApplyUpdate(roofing, request, currentUserId, now);
                break;
            case "fiber":
                var fiber = await db.FiberUserCommissionRates.FindAsync(rateId);
                if (fiber == null) return ApiResults.NotFound("Rate not found");
                ApplyUpdate(fiber, request, currentUserId, now);
                break;
            default:
                return ApiResults.BadRequest("Invalid industry");
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { RateId = rateId, Updated = true });
    }

    private static void ApplyUpdate(dynamic rate, RateUpdateRequest req, int updatedBy, DateTime now)
    {
        if (req.RoleId.HasValue) rate.RoleId = req.RoleId;
        if (req.InstallerId.HasValue) rate.InstallerId = req.InstallerId;
        if (req.StateCode != null) rate.StateCode = req.StateCode;
        if (req.PercentMp1.HasValue) rate.PercentMp1 = req.PercentMp1;
        if (req.FlatMp1.HasValue) rate.FlatMp1 = req.FlatMp1;
        if (req.PercentMp2.HasValue) rate.PercentMp2 = req.PercentMp2;
        if (req.FlatMp2.HasValue) rate.FlatMp2 = req.FlatMp2;
        if (req.IsActive.HasValue) rate.IsActive = req.IsActive.Value;
        if (req.EffectiveStartDate.HasValue) rate.EffectiveStartDate = req.EffectiveStartDate.Value;
        if (req.EffectiveEndDate.HasValue) rate.EffectiveEndDate = req.EffectiveEndDate;
        rate.UpdatedBy = updatedBy;
        rate.UpdatedAt = now;
    }

    private static async Task<IResult> DeleteRate(
        string industry,
        int rateId,
        HttpContext http,
        SparkDbContext db)
    {
        var authResult = EndpointAuthHelpers.RequireAuthority(http, 4, out var currentUserId);
        if (authResult != null) return authResult;

        var now = DateTime.UtcNow;

        switch (industry.ToLower())
        {
            case "solar":
                var solar = await db.SolarUserCommissionRates.FindAsync(rateId);
                if (solar == null) return ApiResults.NotFound("Rate not found");
                solar.IsActive = false; solar.UpdatedBy = currentUserId; solar.UpdatedAt = now;
                break;
            case "pest":
                var pest = await db.PestUserCommissionRates.FindAsync(rateId);
                if (pest == null) return ApiResults.NotFound("Rate not found");
                pest.IsActive = false; pest.UpdatedBy = currentUserId; pest.UpdatedAt = now;
                break;
            case "roofing":
                var roofing = await db.RoofingUserCommissionRates.FindAsync(rateId);
                if (roofing == null) return ApiResults.NotFound("Rate not found");
                roofing.IsActive = false; roofing.UpdatedBy = currentUserId; roofing.UpdatedAt = now;
                break;
            case "fiber":
                var fiber = await db.FiberUserCommissionRates.FindAsync(rateId);
                if (fiber == null) return ApiResults.NotFound("Rate not found");
                fiber.IsActive = false; fiber.UpdatedBy = currentUserId; fiber.UpdatedAt = now;
                break;
            default:
                return ApiResults.BadRequest("Invalid industry");
        }

        await db.SaveChangesAsync();
        return ApiResults.Success(new { RateId = rateId, Deleted = true });
    }
}

// Rate DTOs placed here to keep endpoint-specific
public record RateCreateRequest(
    int UserId, int? RoleId, int? InstallerId, string? StateCode,
    decimal? PercentMp1, decimal? FlatMp1, decimal? PercentMp2, decimal? FlatMp2,
    DateOnly? EffectiveStartDate, DateOnly? EffectiveEndDate);

public record RateUpdateRequest(
    int? RoleId, int? InstallerId, string? StateCode,
    decimal? PercentMp1, decimal? FlatMp1, decimal? PercentMp2, decimal? FlatMp2,
    bool? IsActive, DateOnly? EffectiveStartDate, DateOnly? EffectiveEndDate);
