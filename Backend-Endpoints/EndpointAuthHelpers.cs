using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SparkBackend.Data;
using SparkBackend.Models;
using SparkBackend.Services;
using SparkBackend.Middleware;
using System.Security.Claims;
using BCrypt.Net;

namespace SparkBackend.Endpoints;

public static class EndpointAuthHelpers
{
    public static int? GetAuthenticatedUserId(HttpContext http)
    {
        return http.Session.GetInt32("UserId");
    }

    public static IResult? RequireAuth(HttpContext http, out int userId)
    {
        var id = http.Session.GetInt32("UserId");
        if (!id.HasValue)
        {
            userId = 0;
            return Results.Json(
                new { error = "Authentication required" },
                statusCode: 401
            );
        }
        userId = id.Value;
        return null;
    }

    public static IResult? RequireAuthAndOwnership(HttpContext http, int resourceUserId, out int authenticatedUserId)
    {
        var authResult = RequireAuth(http, out authenticatedUserId);
        if (authResult != null) return authResult;

        if (authenticatedUserId == resourceUserId)
            return null;

        var authorityLevel = http.Session.GetInt32("AuthorityLevel") ?? 1;
        if (authorityLevel >= 5)
            return null;

        return Results.Json(
            new { error = "Access forbidden" },
            statusCode: 403
        );
    }

    /// <summary>
    /// Combines authentication check with minimum authority level requirement.
    /// Returns null on success (userId is set), or an error IResult.
    /// </summary>
    public static IResult? RequireAuthority(HttpContext http, int minimumLevel, out int userId)
    {
        var authResult = RequireAuth(http, out userId);
        if (authResult != null) return authResult;

        var authorityLevel = GetAuthorityLevel(http);
        if (authorityLevel < minimumLevel)
        {
            return Results.Json(
                new { error = "Insufficient authority level" },
                statusCode: 403
            );
        }

        return null;
    }

    /// <summary>
    /// Returns the current session authority level (1-5). Defaults to 1 if not set.
    /// </summary>
    public static int GetAuthorityLevel(HttpContext http)
    {
        return http.Session.GetInt32("AuthorityLevel") ?? 1;
    }
}
