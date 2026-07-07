// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace ASC.Core.Common.EF;

public enum Provider
{
    PostgreSql,
    MySql
}

public class InstallerOptionsAction(string region, string nameConnectionString)
{
    private static Lazy<ServerVersion> _lazyServerVersion;

    public void OptionsAction(IServiceProvider sp, DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationExtension(sp.GetRequiredService<IConfiguration>());
        var migrateAssembly = configuration["testAssembly"];
        var connectionString = configuration.GetConnectionStrings(nameConnectionString, region);
        var loggerFactory = sp.GetRequiredService<EFLoggerFactory>();

        optionsBuilder.UseLoggerFactory(loggerFactory);
        optionsBuilder.EnableSensitiveDataLogging();

        var provider = connectionString.ProviderName switch
        {
            "MySql.Data.MySqlClient" => Provider.MySql,
            "Npgsql" => Provider.PostgreSql,
            _ => Provider.MySql
        };

        switch (provider)
        {
            case Provider.MySql:
                var mysqlVersionString = sp.GetRequiredService<IConfiguration>()["mysqlServerVersion"];
                var serverVersion = !string.IsNullOrEmpty(mysqlVersionString)
                    ? ServerVersion.Parse(mysqlVersionString)
                    : GetOrDetectServerVersion(connectionString.ConnectionString);

                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                optionsBuilder.UseMySql(connectionString.ConnectionString, serverVersion, providerOptions =>
                {
                    if (!string.IsNullOrEmpty(migrateAssembly))
                    {
                        providerOptions.MigrationsAssembly(migrateAssembly);
                    }

                    //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
                    providerOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);

                    providerOptions.UseMicrosoftJson();
                });
                break;
            case Provider.PostgreSql:
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                optionsBuilder.UseNpgsql(connectionString.ConnectionString, providerOptions =>
                {
                    if (!string.IsNullOrEmpty(migrateAssembly))
                    {
                        providerOptions.MigrationsAssembly(migrateAssembly);
                    }
                });
                break;
        }
    }

    private static ServerVersion GetOrDetectServerVersion(string connectionString)
    {
        _lazyServerVersion ??= new Lazy<ServerVersion>(
            () => ServerVersion.AutoDetect(connectionString));

        return _lazyServerVersion.Value;
    }
}

public class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    public override int SaveChanges()
    {
        ValidateEntries();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateEntries();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void ValidateEntries()
    {
        var entities = from e in ChangeTracker.Entries()
                       where e.State is EntityState.Added or EntityState.Modified
                       select e.Entity;
        foreach (var entity in entities)
        {
            List<ValidationResult> results = [];
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity), results, true))
            {
                throw new ArgumentException(results.First().ErrorMessage);
            }
        }
    }
}
public static class BaseDbContextExtension
{
    private const int DefaultPoolSize = 1024;
    private static int? _cachedPoolSize;

    public static IServiceCollection AddBaseDbContextPool<T>(this IServiceCollection services, string region = "current", string nameConnectionString = "default") where T : DbContext
    {
        _cachedPoolSize ??= services
            .FirstOrDefault(d => d.ServiceType == typeof(IConfiguration))
            ?.ImplementationInstance is IConfiguration cfg
            ? cfg.GetValue("core:dbContextPoolSize", DefaultPoolSize)
            : DefaultPoolSize;

        var installerOptionsAction = new InstallerOptionsAction(region, nameConnectionString);
        services.AddPooledDbContextFactory<T>(installerOptionsAction.OptionsAction, _cachedPoolSize.Value);

        return services;
    }

    public static async Task<T> AddOrUpdateAsync<T, TContext>(this TContext b, Expression<Func<TContext, DbSet<T>>> expressionDbSet, T entity) where T : BaseEntity where TContext : DbContext
    {
        var dbSet = expressionDbSet.Compile().Invoke(b);
        var existingBlog = await dbSet.FindAsync(entity.GetKeys());
        if (existingBlog == null)
        {
            var entityEntry = await dbSet.AddAsync(entity);

            return entityEntry.Entity;
        }

        b.Entry(existingBlog).CurrentValues.SetValues(entity);
        b.Entry(existingBlog).State = EntityState.Modified;
        return entity;
    }


    public static async Task<T> AddOrUpdateAsync<T>(this DbSet<T> dbSet, T entity) where T : BaseEntity
    {
        var existingBlog = await dbSet.FindAsync(entity.GetKeys());
        if (existingBlog == null)
        {
            var entityEntry = await dbSet.AddAsync(entity);

            return entityEntry.Entity;
        }

        dbSet.Update(entity);
        return entity;
    }
}

public abstract class BaseEntity
{
    public abstract object[] GetKeys();
}

public class WarmupBaseDbContextStartupTask(IServiceProvider provider, ILogger<WarmupBaseDbContextStartupTask> logger) : IStartupTaskNotAwaitable
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            var name = x.GetName().Name;
            return !string.IsNullOrEmpty(name) && name.StartsWith("ASC.");
        });

        var types = assemblies.SelectMany(r => r.GetTypes().Where(t => t.IsSubclassOf(typeof(DbContext))));

        foreach (var t in types)
        {
            using var scope = provider.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetService(typeof(IDbContextFactory<>).MakeGenericType(t));
            var createDbContextMethod = dbContextFactory?.GetType().GetMethod("CreateDbContext");
            if (createDbContextMethod == null)
            {
                continue;
            }

            var queries = t
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Where(r => !r.IsSpecialName && r.IsDefined(typeof(PreCompileQuery), false));

            foreach (var q in queries)
            {
                try
                {
                    var args = q.GetParameters()
                        .Select(p => GetDefaultValue(p.ParameterType))
                        .ToArray();

                    var context = createDbContextMethod.Invoke(dbContextFactory, null);
                    if (context == null)
                    {
                        continue;
                    }

                    await using (context as IAsyncDisposable)
                    {
                        var dbContext = (DbContext)context;
                        var strategy = dbContext.Database.CreateExecutionStrategy();
                        await strategy.ExecuteAsync(async () =>
                        {
                            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                            try
                            {
                                await ExecuteQueryAsync(q.Invoke(context, args), cancellationToken);
                            }
                            finally
                            {
                                await tx.RollbackAsync(cancellationToken);
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, q.Name);
                }
            }
        }
    }

    private static readonly MethodInfo _drainMethod =
        typeof(WarmupBaseDbContextStartupTask).GetMethod(nameof(DrainAsync), BindingFlags.Static | BindingFlags.NonPublic);

    private static async Task ExecuteQueryAsync(object result, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case null:
                return;
            case Task task:
                await task.ConfigureAwait(false);
                return;
        }

        var asyncEnumerable = result.GetType().GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        if (asyncEnumerable != null)
        {
            var drain = _drainMethod.MakeGenericMethod(asyncEnumerable.GetGenericArguments()[0]);
            var t = drain.Invoke(null, [result, cancellationToken]);
            if (t is Task task)
            {
                await task;
            }
        }
    }

    // Enumerating the first element forces the compiled streaming query to translate to SQL and execute.
    private static async Task DrainAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync().ConfigureAwait(false);
    }

    private static object GetDefaultValue(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string))
        {
            return null;
        }
        if (t == typeof(int))
        {
            return int.MaxValue;
        }
        if (t == typeof(long))
        {
            return long.MaxValue;
        }
        if (t == typeof(Guid))
        {
            return Guid.Empty;
        }
        if (t == typeof(DateTime))
        {
            return DateTime.MinValue;
        }
        if (t.IsEnum)
        {
            return Enum.GetValues(t).GetValue(0);
        }

        var enumerable = t.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerable != null)
        {
            return Array.CreateInstance(enumerable.GetGenericArguments()[0], 0);
        }

        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class PreCompileQuery : Attribute;
