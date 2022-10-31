using Common.Logging;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Shopping.Aggregator.Services;
using System;
using System.Net.Http;

namespace Shopping.Aggregator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddTransient<LoggingDelegatingHandler>();


            services.AddHttpClient<ICatalogService, CatalogService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:CatalogUrl"]))
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy())  
                .AddPolicyHandler(GetCircuitBreakerPolicy()); 



            services.AddHttpClient<IBasketService, BasketService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:BasketUrl"]))
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                //.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2))) //retry pattern with polly 3 retry with 2 sec interval
                //.AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));//circuit breaker pattern with polly 5 times with 30 sec. intervals 
                .AddPolicyHandler(GetRetryPolicy())  //Advanced retry policy pattern -> AddPolicyHandler is an extension method provided by HttpClient
                .AddPolicyHandler(GetCircuitBreakerPolicy()); //Advanced circuit breaker policy pattern -> AddPolicyHandler is an extension method provided by HttpClient  


            services.AddHttpClient<IOrderService, OrderService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:OrderingUrl"]))
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());


            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shopping.Aggregator", Version = "v1" });
            });

            //Add Health check
            services.AddHealthChecks()
                    //sub health check the target uri is healthy or not
                    .AddUrlGroup(new Uri($"{Configuration["ApiSettings:CatalogUrl"]}/swagger/index.html"), "Catalog.API", HealthStatus.Degraded)
                    .AddUrlGroup(new Uri($"{Configuration["ApiSettings:BasketUrl"]}/swagger/index.html"), "Basket.API", HealthStatus.Degraded)
                    .AddUrlGroup(new Uri($"{Configuration["ApiSettings:OrderingUrl"]}/swagger/index.html"), "Ordering.API", HealthStatus.Degraded);

        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            // Retry a specified number of times, using a function to
            // calculate the duration to wait between retries based on
            // the current retry attempt (allows for exponential back-off)
            // In this case will wait for
            //  2 ^ 1 = 2 seconds then
            //  2 ^ 2 = 4 seconds then
            //  2 ^ 3 = 8 seconds then
            //  2 ^ 4 = 16 seconds then
            //  2 ^ 5 = 32 seconds
            return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        retryCount: 5,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (exception, retryCount, context) =>
                        {
                            Log.Error($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                        });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                   .HandleTransientHttpError()
                   .CircuitBreakerAsync(
                       handledEventsAllowedBeforeBreaking: 5,
                       durationOfBreak: TimeSpan.FromSeconds(30)
                   );
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopping.Aggregator v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapHealthChecks("/hc");
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}
