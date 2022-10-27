using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common.Logging;
//using AspnetRunBasics.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace AspnetRunBasics
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            //SeedDatabase(host);
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                //.UseSerilog((context, configuration) =>
                //{
                //    configuration
                //        .Enrich.FromLogContext()
                //        .Enrich.WithMachineName()
                //        .WriteTo.Console()
                //        .WriteTo.Elasticsearch(
                //            new ElasticsearchSinkOptions(new Uri(context.Configuration["ElasticConfiguration:Uri"]))
                //            {
                //                IndexFormat = $"applogs-{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-logs-{DateTime.UtcNow:yyyy-MM}",
                //                AutoRegisterTemplate = true,
                //                NumberOfShards = 2,
                //                NumberOfReplicas = 1
                //            })
                //        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                //        .ReadFrom.Configuration(context.Configuration);
                //})
                .UseSerilog(SeriLogger.Configure)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        //private static void SeedDatabase(IHost host)
        //{
        //    using (var scope = host.Services.CreateScope())
        //    {
        //        var services = scope.ServiceProvider;
        //        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        //        try
        //        {
        //            var aspnetRunContext = services.GetRequiredService<AspnetRunContext>();
        //            AspnetRunContextSeed.SeedAsync(aspnetRunContext, loggerFactory).Wait();
        //        }
        //        catch (Exception exception)
        //        {
        //            var logger = loggerFactory.CreateLogger<Program>();
        //            logger.LogError(exception, "An error occurred seeding the DB.");
        //        }
        //    }
        //}
    }
}
