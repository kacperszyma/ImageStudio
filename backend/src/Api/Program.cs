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

var app = builder.Build();

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
    IGenerationService generationService,
    IGenerationManager generationManager) =>
{
    // The host owns the web framework: pull the raw bytes and headers off the
    // request into a transport-neutral envelope, then hand it to Generation,
    // which owns the provider's wire format and signature scheme.
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    var headers = request.Headers.ToDictionary(
        h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var webhook = new WebhookRequest(buffer.ToArray(), headers);

    GenerationCallback callback;
    try
    {
        callback = await generationService.ParseCallbackAsync(webhook);
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

app.MapGet("/transactions", async (HttpContext ctx, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await walletService.GetTransactionsAsync(userId));
}).RequireAuthorization();

app.MapGet("/transactions/{id:guid}", async (Guid id, IWalletService walletService) =>
{
    var tx = await walletService.GetTransactionAsync(id);
    if (tx is null) return Results.NotFound();
    if (tx.GenerationJobId is { } jobId)
        return Results.Redirect($"/generations/{jobId}");
    return Results.Ok(tx);
}).RequireAuthorization();

app.MapGet("/generations/{id:guid}", async (Guid id, IGenerationManager generationManager) =>
{
    var details = await generationManager.GetDetailsAsync(id);
    return details is null ? Results.NotFound() : Results.Ok(details);
}).RequireAuthorization();

app.Run();








