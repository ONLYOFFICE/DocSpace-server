// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Api.Core.Extensions;

namespace ASC.Core.Common.EF;

public enum Provider
{
    PostgreSql,
    MySql
}

public class InstallerOptionsAction(string region, string nameConnectionString)
{
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
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMySqlMigrationsSqlGenerator>();
                optionsBuilder.UseMySql(connectionString.ConnectionString, ServerVersion.AutoDetect(connectionString.ConnectionString), providerOptions =>
                {
                    if (!string.IsNullOrEmpty(migrateAssembly))
                    {
                        providerOptions.MigrationsAssembly(migrateAssembly);
                    }

                    //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                    providerOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
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
}

public static class BaseDbContextExtension
{
    public static IServiceCollection AddBaseDbContextPool<T>(this IServiceCollection services, string region = "current", string nameConnectionString = "default") where T : DbContext
    {
        var installerOptionsAction = new InstallerOptionsAction(region, nameConnectionString);
        services.AddPooledDbContextFactory<T>(installerOptionsAction.OptionsAction);

        return services;
    }

    public static T AddOrUpdate<T, TContext>(this TContext b, DbSet<T> dbSet, T entity) where T : BaseEntity where TContext : DbContext
    {
        var keys = entity.GetKeys();
        var existingBlog = dbSet.Find(keys);
        if (existingBlog == null)
        {
            return dbSet.Add(entity).Entity;
        }

        b.Entry(existingBlog).CurrentValues.SetValues(entity);
        b.Entry(existingBlog).State = EntityState.Modified;
        return entity;
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

            var queries = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Where(r => !r.IsSpecialName);

            foreach (var q in queries)
            {
                try
                {
                    var @params = q.GetParameters();
                    var paramsAttr = q.GetCustomAttribute<PreCompileQuery>();
                    
                    if (paramsAttr == null || paramsAttr.Data.Length != @params.Length)
                    {
                        continue;
                    }
                    
                    var paramsToInvoke = new List<object>(@params.Length);
                    
                    for (var i = 0; i < @params.Length; i++)
                    {
                        var p = paramsAttr.Data[i];
                        if (@params[i].ParameterType == typeof(Guid))
                        {
                            if (Guid.TryParse(p.ToString(), out var g))
                            {
                                paramsToInvoke.Add(g);
                            }
                        }
                        else if (@params[i].ParameterType == typeof(DateTime))
                        {
                            if (DateTime.TryParse(p.ToString(), CultureInfo.InvariantCulture, out var d))
                            {
                                paramsToInvoke.Add(d);
                            }
                        }
                        else
                        {
                            paramsToInvoke.Add(p);
                        }
                    }
                    
                    var context = createDbContextMethod.Invoke(dbContextFactory, null);
                    if (context == null)
                    {
                        continue;
                    }
                    
                    var res = q.Invoke(context, paramsToInvoke.ToArray());
                    if (res is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }
                    
                    var disposeContext = context.GetType().GetMethod("Dispose");
                    if (disposeContext == null)
                    {
                        continue;
                    }
                    disposeContext.Invoke(context, null);
                }
                catch (Exception e)
                {
                    logger.LogDebug(e, q.Name);
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class PreCompileQuery(object[] data) : Attribute
{
    public object[] Data { get; } = data;

    public const int DefaultInt = int.MaxValue;
    public const string DefaultGuid = "00000000-0000-0000-0000-000000000000";
    public const string DefaultDateTime = "01/01/0001 00:00:00";
}
