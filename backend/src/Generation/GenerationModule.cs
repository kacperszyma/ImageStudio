using Generation.Contracts;
using Generation.Fal;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Generation;

public static class GenerationModule
{
    public static IServiceCollection AddGenerationModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<GenerationDbContext>(opt =>
            opt.UseNpgsql(config["DATABASE_CONNECTION_STRING"]));
        services.AddHttpClient<FalClient>();
        services.AddHttpClient(); // IHttpClientFactory for the verifier's JWKS fetch
        services.AddSingleton<FalWebhookVerifier>(); // caches Fal's public keys
        services.AddSingleton<FalMetrics>();
        services.AddScoped<IGenerationProvider,FalGenerationProvider>();
       // services.AddScoped<IGenerationProvider, MockGenerationProvider>();
        services.AddSingleton<ICloudBucket>(_ =>
        {
            var bucket = config["GCS_BUCKET_NAME"]
                ?? throw new InvalidOperationException("GCS_BUCKET_NAME is not configured.");

            // STORAGE_EMULATOR_HOST is set only in local dev (see .env) — when
            // present, the GCS client library talks to the fake-gcs-server
            // container (docker-compose) instead of real GCS. Unset in prod,
            // where this same call authenticates via Cloud Run's attached
            // service account (Application Default Credentials).
            var emulatorHost = Environment.GetEnvironmentVariable("STORAGE_EMULATOR_HOST");
            var client = new StorageClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.Build();

            // fake-gcs-server has no real IAM to sign against, so only build a
            // signer against real GCS credentials.
            var signer = emulatorHost is null
                ? UrlSigner.FromCredential(GoogleCredential.GetApplicationDefault())
                : null;

            return new GcsCloudBucket(client, bucket, signer, emulatorHost);
        });
        services.AddScoped<GenerationService>();
        services.AddScoped<IGenerationService>(sp => sp.GetRequiredService<GenerationService>());
        services.AddScoped<IGenerationQueryService>(sp => sp.GetRequiredService<GenerationService>());
        services.AddScoped<IGenerationWebhook>(sp => sp.GetRequiredService<GenerationService>());
        return services;
    }

    public static Task ApplyMigrationsAsync(IServiceProvider sp) =>
        sp.GetRequiredService<GenerationDbContext>().Database.MigrateAsync();
}