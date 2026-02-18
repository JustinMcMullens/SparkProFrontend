using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SparkBackend.Data;

namespace SparkBackend.Endpoints;

public static class TenantDbEndpoints
{
    public static void MapTenantDbEndpoints(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SparkDbContext>();
        var model = db.Model;

        var mapMethod = typeof(TenantDbEndpoints).GetMethod(
            nameof(MapTenantCrudFor),
            BindingFlags.NonPublic | BindingFlags.Static
        );

        foreach (var entityType in model.GetEntityTypes())
        {
            var key = entityType.FindPrimaryKey();
            if (key == null)
                continue;

            if (entityType.GetViewName() != null)
                continue;

            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
                continue;

            var generic = mapMethod?.MakeGenericMethod(entityType.ClrType);
            generic?.Invoke(null, new object[] { app, tableName });
        }
    }

    private static void MapTenantCrudFor<TEntity>(WebApplication app, string tableName)
        where TEntity : class
    {
        var group = app.MapGroup($"/api/db/{tableName}")
            .WithTags($"TenantDb:{tableName}")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", async (SparkDbContext dbContext) =>
        {
            var list = await dbContext.Set<TEntity>()
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(list);
        });

        group.MapGet("/{id}", async (string id, SparkDbContext dbContext) =>
        {
            var keyProp = GetSingleKeyProperty<TEntity>(dbContext);
            if (keyProp == null)
            {
                return Results.BadRequest(new
                {
                    error = "Composite keys are not supported for this endpoint.",
                    table = tableName
                });
            }

            if (!TryConvertKey(id, keyProp.ClrType, out var keyValue))
            {
                return Results.BadRequest(new { error = $"Invalid id for {tableName}." });
            }

            var entity = await dbContext.FindAsync<TEntity>(new[] { keyValue! });
            return entity == null ? Results.NotFound() : Results.Ok(entity);
        });

        group.MapPost("/", async (TEntity entity, SparkDbContext dbContext) =>
        {
            dbContext.Set<TEntity>().Add(entity);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/api/db/{tableName}", entity);
        });

        group.MapPut("/{id}", async (string id, TEntity entity, SparkDbContext dbContext) =>
        {
            var keyProp = GetSingleKeyProperty<TEntity>(dbContext);
            if (keyProp == null)
            {
                return Results.BadRequest(new
                {
                    error = "Composite keys are not supported for this endpoint.",
                    table = tableName
                });
            }

            if (!TryConvertKey(id, keyProp.ClrType, out var keyValue))
            {
                return Results.BadRequest(new { error = $"Invalid id for {tableName}." });
            }

            var existing = await dbContext.FindAsync<TEntity>(new[] { keyValue! });
            if (existing == null)
                return Results.NotFound();

            keyProp.PropertyInfo?.SetValue(entity, keyValue);
            dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await dbContext.SaveChangesAsync();
            return Results.Ok(existing);
        });

        group.MapDelete("/{id}", async (string id, SparkDbContext dbContext) =>
        {
            var keyProp = GetSingleKeyProperty<TEntity>(dbContext);
            if (keyProp == null)
            {
                return Results.BadRequest(new
                {
                    error = "Composite keys are not supported for this endpoint.",
                    table = tableName
                });
            }

            if (!TryConvertKey(id, keyProp.ClrType, out var keyValue))
            {
                return Results.BadRequest(new { error = $"Invalid id for {tableName}." });
            }

            var entity = await dbContext.FindAsync<TEntity>(new[] { keyValue! });
            if (entity == null)
                return Results.NotFound();

            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IProperty? GetSingleKeyProperty<TEntity>(DbContext dbContext)
        where TEntity : class
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
        var key = entityType?.FindPrimaryKey();
        if (key == null || key.Properties.Count != 1)
            return null;

        return key.Properties[0];
    }

    private static bool TryConvertKey(string raw, Type keyType, out object? value)
    {
        value = null;
        var targetType = Nullable.GetUnderlyingType(keyType) ?? keyType;

        if (targetType == typeof(string))
        {
            value = raw;
            return true;
        }

        if (targetType == typeof(Guid))
        {
            if (Guid.TryParse(raw, out var guid))
            {
                value = guid;
                return true;
            }
            return false;
        }

        if (targetType == typeof(DateOnly))
        {
            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            {
                value = dateOnly;
                return true;
            }
            return false;
        }

        if (targetType == typeof(DateTime))
        {
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                value = dateTime;
                return true;
            }
            return false;
        }

        try
        {
            value = Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
