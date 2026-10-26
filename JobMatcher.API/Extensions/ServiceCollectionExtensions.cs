using System.Text;
using JobMatcher.API.Configuration;
using JobMatcher.API.Services;
using JobMatcher.API.Services.Ai;
using JobMatcher.API.Services.Interfaces;
using JobMatcher.API.Services.JustJoinIt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JobMatcher.API.Extensions;

public static class ServiceCollectionExtensions
{
    // LLM calls are slow: allow up to a minute per attempt, retries included in the total
    private static readonly TimeSpan LlmAttemptTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LlmTotalTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Registers the AI scoring and CV parsing services. The provider is picked by "Ai:Provider"
    /// (Claude by default); only its options are required.
    /// </summary>
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration config)
    {
        var provider = config.GetSection(AiOptions.SectionName).Get<AiOptions>()?.Provider ?? AiProvider.Claude;

        var httpClient = provider switch
        {
            AiProvider.Claude => services
                .AddValidatedOptions<ClaudeOptions>(ClaudeOptions.SectionName)
                .AddHttpClient<ILlmClient, ClaudeLlmClient>((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", options.ApiVersion);
                }),

            AiProvider.Gemini => services
                .AddValidatedOptions<GeminiOptions>(GeminiOptions.SectionName)
                .AddHttpClient<ILlmClient, GeminiLlmClient>((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    // Header instead of the ?key= query parameter, so the key never shows up in URL logs
                    client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
                }),

            _ => throw new InvalidOperationException($"Unknown AI provider: {provider}")
        };

        // Retries 429/5xx with backoff and honors Retry-After; HttpClient.Timeout is left to the pipeline
        httpClient
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = LlmAttemptTimeout;
                options.TotalRequestTimeout.Timeout = LlmTotalTimeout;
                options.CircuitBreaker.SamplingDuration = LlmAttemptTimeout * 2;
            });

        services.AddScoped<IAiScoringService, AiScoringService>();
        services.AddScoped<IAiCvParserService, AiCvParserService>();

        return services;
    }

    public static IServiceCollection AddJobBoard(this IServiceCollection services)
    {
        services
            .AddValidatedOptions<JobBoardOptions>(JobBoardOptions.SectionName)
            .AddHttpClient<IJobFetcherService, JustJoinItFetcherService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<JobBoardOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);

                // JustJoinIT rejects requests that do not look like they come from a browser
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Linux; Android 15; Pixel 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Mobile Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
                client.DefaultRequestHeaders.Add("Referer", "https://justjoin.it/job-offers/all-locations");
            })
            .AddStandardResilienceHandler();

        services.AddScoped<IJobScoringOrchestrator, JobScoringOrchestrator>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddValidatedOptions<JwtOptions>(JwtOptions.SectionName);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
                };
            });

        services.AddAuthorization();
        services.AddScoped<AuthService>();

        return services;
    }

    /// <summary>Binds options from configuration and fails at startup if they are invalid.</summary>
    private static IServiceCollection AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
