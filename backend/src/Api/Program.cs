using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Wallet;
using Users;
using Generation;
using GenerationManager;
using Generation.Contracts;
using SharedKernel;
using Api.Middleware;
using Api.Hubs;
using GenerationManager.Contracts;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Wallet.Contracts;

DotNetEnv.Env.Load("../../../.env");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddScoped<IGenerationNotifier, Api.Hubs.SignalRGenerationNotifier>();
builder.Services.AddWalletModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddGenerationModule(builder.Configuration);
builder.Services.AddGenerationManagerModule(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "Frontend",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});
builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.Authority = "https://dev-yw7pijmj3lf7zgrf.us.auth0.com/";
            options.Audience = "https://imagestudio-api";
            options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/generate"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });
builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
        {
            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        }));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    await UsersModule.ApplyMigrationsAsync(sp);
    await WalletModule.ApplyMigrationsAsync(sp);
    await GenerationModule.ApplyMigrationsAsync(sp);
    await GenerationManagerModule.ApplyMigrationsAsync(sp);
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseProvisioning();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", opt => opt.WithTitle("API Routes"));
}

app.MapHub<GenerationHub>("/generate");

// Fal posts generation results here. Path is derived from the same config
// value we hand to Fal as ?fal_webhook=, so the endpoint and the registration
// can never drift apart.
var falWebhookPath = new Uri(app.Configuration["FAL_WEBHOOK_URL"]
    ?? throw new InvalidOperationException("FAL_WEBHOOK_URL is not configured.")).AbsolutePath;

app.MapPost(falWebhookPath, async (
    HttpRequest request,
    IGenerationWebhook generationWebhook,
    IGenerationManager generationManager) =>
{
    WebhookRequest webhook = await ExtractWebhook(request);

    GenerationCallback callback;
    try
    {
        callback = await generationWebhook.ParseCallbackAsync(webhook);
    }
    catch (WebhookVerificationException)
    {
        // Not authentic: reject and process nothing. Don't 200 — a forged
        // request is not an "already handled" one.
        return Results.Unauthorized();
    }

    await generationManager.CompleteJobAsync(callback.RequestId, callback.ImageUrl, callback.Success);

    // Always 200 so Fal stops retrying; CompleteJobAsync is idempotent anyway.
    return Results.Ok();
});

app.MapPost("stripe/webhook", async (HttpRequest request, IWalletService walletService) =>
{
    WebhookRequest webhook = await ExtractWebhook(request);

    var signature = request.Headers["Stripe-Signature"].ToString();
    var payload = System.Text.Encoding.UTF8.GetString(webhook.Body);

    try
    {
        await walletService.ProcessPaymentWebhookAsync(payload, signature);
    }
    catch (WebhookVerificationException)
    {
        return Results.Unauthorized();
    }

    return Results.Ok();
});

app.MapGet("/models", (IGenerationService generationService) =>
{
    return Results.Ok(generationService.GetModels());
});
app.MapGet("/balance", async (HttpContext ctx, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await walletService.GetBalanceAsync(userId));
}).RequireAuthorization();

app.MapGet("/history", async (HttpContext ctx, IGenerationManager generationManager) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await generationManager.GetHistoryAsync(userId));
}).RequireAuthorization();

app.MapGet("/history/{id:guid}", (Guid id) => Results.Redirect($"/generations/{id}"))
    .RequireAuthorization();

app.MapGet("/packages", (IWalletService walletService) =>
{
    return Results.Ok(walletService.GetPackages());
});

app.MapPost("/checkout", async (HttpContext ctx, IWalletService walletService, CheckoutRequest body) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    var clientSecret = await walletService.CreateCheckoutAsync(userId, body.PackageId);
    return Results.Ok(new { clientSecret });
}).RequireAuthorization();

app.MapPost("/checkout/redeem", async (HttpContext ctx, IWalletService walletService, RedeemRequest body) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    await walletService.RedeemSessionAsync(body.SessionId, userId);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/spend", async (HttpContext ctx, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await walletService.GetSpendingHistoryAsync(userId));
}).RequireAuthorization();

app.MapGet("/spend/{id:guid}", async (HttpContext ctx, Guid id, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    var tx = await walletService.GetTransactionAsync(id, userId);
    if (tx is null) return Results.NotFound();
    if (tx.GenerationJobId is { } jobId)
        return Results.Redirect($"/generations/{jobId}");
    return Results.Ok(tx);
}).RequireAuthorization();

app.MapGet("/transactions", async (HttpContext ctx, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await walletService.GetPurchasesAsync(userId));
}).RequireAuthorization();

app.MapGet("/generations/{id:guid}", async (HttpContext ctx, Guid id, IGenerationManager generationManager) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    var details = await generationManager.GetDetailsAsync(id, userId);
    return details is null ? Results.NotFound() : Results.Ok(details);
}).RequireAuthorization();

app.Run();

async Task<WebhookRequest> ExtractWebhook(HttpRequest httpRequest)
{
    using var buffer = new MemoryStream();
    await httpRequest.Body.CopyToAsync(buffer);

    var headers = httpRequest.Headers.ToDictionary(
        h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var webhookRequest = new WebhookRequest(buffer.ToArray(), headers);
    return webhookRequest;
}

record CheckoutRequest(string PackageId);
record RedeemRequest(string SessionId);








