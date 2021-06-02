using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using ASC.Common.Caching;
using ASC.Common;
using ASC.Common.DependencyInjection;
using ASC.Common.Logging;
using ASC.Common.Utils;
using ASC.Web.Files.Core.Search;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ASC.CRM
{
    public class Program
    {
        public async static System.Threading.Tasks.Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseWindowsService()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var builder = webBuilder.UseStartup<Startup>();

                    builder.ConfigureKestrel((hostingContext, serverOptions) =>
                    {
                        var kestrelConfig = hostingContext.Configuration.GetSection("Kestrel");

                        if (!kestrelConfig.Exists()) return;

                        var unixSocket = kestrelConfig.GetValue<string>("ListenUnixSocket");

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            if (!String.IsNullOrWhiteSpace(unixSocket))
                            {
                                unixSocket = String.Format(unixSocket, hostingContext.HostingEnvironment.ApplicationName.Replace("ASC.", "").Replace(".", ""));

                                serverOptions.ListenUnixSocket(unixSocket);
                            }
                        }
                    });
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var buided = config.Build();
                    var path = buided["pathToConf"];
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.GetFullPath(CrossPlatform.PathCombine(hostingContext.HostingEnvironment.ContentRootPath, path));
                    }

                    config.SetBasePath(path);
                    config
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true)
                    .AddJsonFile("storage.json")
                    .AddJsonFile("kafka.json")
                    .AddJsonFile($"kafka.{hostingContext.HostingEnvironment.EnvironmentName}.json", true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                                        {"pathToConf", path}
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();

                    var diHelper = new DIHelper(services);

                    diHelper.TryAdd(typeof(ICacheNotify<>), typeof(KafkaCache<>));

                    diHelper.RegisterProducts(hostContext.Configuration, hostContext.HostingEnvironment.ContentRootPath);
                    //services.AddHostedService<ServiceLauncher>();
                    //diHelper.TryAdd<ServiceLauncher>();

                    //services.AddHostedService<FeedAggregatorService>();
                    //diHelper.TryAdd<FeedAggregatorService>();

                    //services.AddHostedService<Launcher>();
                    //diHelper.TryAdd<Launcher>();

                    LogNLogExtension.ConfigureLog(diHelper, "ASC.Files", "ASC.Feed.Agregator");
                    //diHelper.TryAdd<FileConverter>();
                    diHelper.TryAdd<FactoryIndexerFile>();
                    diHelper.TryAdd<FactoryIndexerFolder>();
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    builder.Register(context.Configuration, true, false);
                });//if (!FilesIntegration.IsRegisteredFileSecurityProvider("crm", "crm_common"))//{//    FilesIntegration.RegisterFileSecurityProvider("crm", "crm_common", new FileSecurityProvider());//}////Register prodjects' calendar events//CalendarManager.Instance.RegistryCalendarProvider(userid =>//{//    if (WebItemSecurity.IsAvailableForUser(WebItemManager.CRMProductID, userid))//    {//        return new List<BaseCalendar> { new CRMCalendar(userid) };//    }//    return new List<BaseCalendar>();//});
    }
}
