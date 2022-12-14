using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Discount.Grpc.Extensions;
using Serilog;
using Common.Logging;

namespace Discount.Grpc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();
            
            var host = CreateHostBuilder(args).Build();
            host.MigrateDatabase<Program>();
            host.Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .UseSerilog(SeriLogger.Configure)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
