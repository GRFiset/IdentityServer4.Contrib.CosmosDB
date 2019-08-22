using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Host.Configuration;
using IdentityServer4.Contrib.CosmosDB.Entities;
using IdentityServer4.Contrib.CosmosDB.Extensions;
using IdentityServer4.Contrib.CosmosDB.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Host
{
    public class Program
    {
        public static string AppName = Assembly.GetEntryAssembly().GetName().Name;
        public static Version AppVersion = Assembly.GetEntryAssembly().GetName().Version;

        public static IConfiguration Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                true)
            .AddEnvironmentVariables()
            .Build();

        public static async Task<int> Main(string[] args)
        {
            Console.Title = $"{AppName}-v{AppVersion}";

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
                .CreateLogger();

            try
            {
                Log.Information($"Starting up {Console.Title}...");

                IWebHost webHost = CreateWebHostBuilder(args);

                // Setup Databases
                using (IServiceScope serviceScope = webHost.Services.CreateScope())
                {
                    await EnsureSeedData(serviceScope.ServiceProvider.GetService<IConfigurationDbContext>(),
                                         serviceScope.ServiceProvider.GetService<IPersistedGrantDbContext>());
                }

                await webHost.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHost CreateWebHostBuilder(string[] args) => WebHost.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseStartup<Startup>()
            .Build();

        private static async Task EnsureSeedData(IConfigurationDbContext configContext, IPersistedGrantDbContext persistedGrantContext)
        {
            await configContext.EnsureConfigurationsCollectionCreated();
            await persistedGrantContext.SetupPersistedGrants();

            foreach (IdentityServer4.Models.Client client in Clients.Get().ToList())
            {
                var dbRecords = await configContext.GetDocument<Client>(x => x.ClientId == client.ClientId);
                if (dbRecords.ToList().Count == 0) await configContext.AddDocument(client.ToEntity());
            }

            foreach (IdentityServer4.Models.IdentityResource resource in Resources.GetIdentityResources().ToList())
            {
                var dbRecords = await configContext.GetDocument<IdentityResource>(x => x.Name == resource.Name);
                if (dbRecords.ToList().Count == 0) await configContext.AddDocument(resource.ToEntity());
            }

            foreach (IdentityServer4.Models.ApiResource resource in Resources.GetApiResources().ToList())
            {
                var dbRecords = await configContext.GetDocument<ApiResource>(x => x.Name == resource.Name);
                if (dbRecords.ToList().Count == 0) await configContext.AddDocument(resource.ToEntity());
            }
        }
    }
}