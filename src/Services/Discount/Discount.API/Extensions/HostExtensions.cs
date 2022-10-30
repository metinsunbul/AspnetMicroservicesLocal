using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using System;
using System.Linq;


namespace Discount.API.Extensions
{
    public static class HostExtensions
    {
        //Note Migration and seeding class

        //public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        public static IHost MigrateDatabase<TContext>(this IHost host)
        {
            //int retryForAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating postgresql database.");

                    var retry = Policy.Handle<NpgsqlException>()
                        .WaitAndRetry(
                           retryCount: 5,
                           sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),//2,4,8,16,32 sec
                            onRetry: (exception, retryCount, context) =>
                            {
                                logger.LogError($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                            });

                    retry.Execute(() => ExecuteMigrations(configuration));

                    //ExecuteMigrations(configuration);

                    logger.LogInformation("Migrated postgresql database.");

                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the postgresql database");

                    //remove manually retry polly policy
                    //if (retryForAvailability < 50) //Convert this retry operation using polly for the microservices resilience.(It helps to make resilience of microservices with creating policies on retry and circut-breaker design patterns )
                    //{
                    //    retryForAvailability++;
                    //    System.Threading.Thread.Sleep(2000);
                    //    MigrateDatabase<TContext>(host, retryForAvailability);
                    //}
                }
            }

            return host;
        }

        private static void ExecuteMigrations(IConfiguration configuration)
        {
            using var connection = new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
            connection.Open();
            using var command = new NpgsqlCommand
            {
                Connection = connection
            };
            command.CommandText = "DROP TABLE IF EXISTS Coupon";
            command.ExecuteNonQuery();

            command.CommandText = @"CREATE TABLE Coupon(Id SERIAL PRIMARY KEY,
                                                                ProductName VARCHAR(24) NOT NULL,
                                                                Description TEXT,
                                                                Amount INT)";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES ('IPhone X', 'IPhone Discount', 150);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES ('Samsung 10', 'Samsung Discount', 100);";
            command.ExecuteNonQuery();
        }


    }
}
