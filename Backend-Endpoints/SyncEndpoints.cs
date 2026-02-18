using SparkBackend.Services;
using System.Reflection;

namespace SparkBackend.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        // POST /sync/{company} - full sync
        app.MapPost("/sync/{company}", async (string company, CompanySyncFactory factory) =>
        {
            var service = factory.GetSyncService(company);
            if (service == null) return Results.NotFound($"No sync service for '{company}'");

            await service.SyncAsync();
            return Results.Ok($"Full sync completed for {company}.");
        });

        // POST /sync/{company}/{datatype} - specific method like SyncUsersAsync
        app.MapPost("/sync/{company}/{type}", async (
            string company,
            string type,
            CompanySyncFactory factory
        ) =>
        {
            var service = factory.GetSyncService(company);
            if (service == null) return Results.NotFound($"No sync service for '{company}'");

            var methodName = $"Sync{char.ToUpper(type[0]) + type.Substring(1)}Async";
            var method = service.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            if (method == null) return Results.NotFound($"No sync method '{methodName}' in {company} service");

            var result = method.Invoke(service, null);
            if (result is Task task) await task;

            return Results.Ok($"{company} {type} sync completed.");
        });
    }
}
