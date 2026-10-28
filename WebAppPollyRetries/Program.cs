using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;
using System.Threading.RateLimiting;

namespace WebAppPollyBulkHead
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpClient<WeatherApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.weather.com");
            })
                .AddResilienceHandler("WeatherApiPolicy", resilienceBuilder =>
                {
                    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

                    // 1. Add Bulkhead Isolation (Concurrency Limiter)
                    // Acts as your bulkhead: restricts maximum parallel calls to 10, 
                    // and allows up to 5 requests to queue up before failing fast.

                    resilienceBuilder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 10,       // Max concurrent requests allowed (like MaxParallelization)
                        QueueLimit = 5          // Max requests allowed to wait in line (like MaxQueuingActions)
                    });


                    // 2. Add a Timeout strategy so hanging requests don't lock up your execution slots indefinitely
                    resilienceBuilder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(5)
                    });

                    // 3. Add Retry strategy for transient network/server failures
                    resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true, // Prevents "thundering herd" spikes
                        ShouldRetryAfterHeader = true,

                        OnRetry = args =>
                        {
                            logger.LogInformation($"[WeatherApi] Retry attempt {args.AttemptNumber} due to {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}.");
                            return ValueTask.CompletedTask;
                        }
                    });


                    builder.Services.AddControllers();
                    var app = builder.Build();

                    app.UseHttpsRedirection();

                    app.UseAuthorization();

                    app.MapControllers();

                    app.Run();
                });
        }
    }
}

