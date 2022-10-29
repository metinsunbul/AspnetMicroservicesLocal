using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace Ordering.API.Extensions
{
    public static class HostExtensions
    {
        //public static IHost MigrateDatabase<TContext>(this IHost host, 
        //                        Action<TContext, IServiceProvider> seeder, 
        //                        int? retry = 0) where TContext : DbContext
        public static IHost MigrateDatabase<TContext>(this IHost host,
                               Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            //int retryForAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var service = scope.ServiceProvider;
                var logger = service.GetRequiredService<ILogger<TContext>>();
                var context = service.GetService<TContext>();

                try
                {
                    logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);


                    var retry = Policy.Handle<SqlException>()
                        .WaitAndRetry(
                           retryCount: 5,
                           sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),//2,4,8,16,32 sec
                            onRetry: (exception, retryCount, context) =>
                            {
                                logger.LogError($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                            });

                    retry.Execute(() => IvokeSeeder(seeder, context, service));

                    //this will handle migration operation and also call the seeder method
                    //IvokeSeeder(seeder, context, service);

                    logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);

                }
                catch (SqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
                    //With using poly retry there is no need to manually apply retry pattern
                    //if (retryForAvailability < 50)
                    //{
                    //    retryForAvailability++;
                    //    System.Threading.Thread.Sleep(2000);
                    //    MigrateDatabase<TContext>(host, seeder, retryForAvailability);
                    //}
                }
            }

            return host;

        }

        private static void IvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, 
                                                    TContext context, 
                                                    IServiceProvider services)
                                                    where TContext : DbContext
        {
            context.Database.Migrate();
            seeder(context, services);
        }


    }
}
