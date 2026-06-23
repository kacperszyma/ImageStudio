using Wallet;
using Users;
using Generation;
using GenerationManager;
using Generation.Contracts;
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
    // The host owns the web framework: pull the raw bytes off the request and
    // hand them to Generation, which owns the provider's wire format.
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    var callback = generationService.ParseCallback(buffer.ToArray());
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

app.MapGet("/history", async (HttpContext ctx, IGenerationService generationService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await generationService.GetGenerationHistory(userId));
}).RequireAuthorization();

app.MapGet("/transactions", async (HttpContext ctx, IWalletService walletService) =>
{
    var userId = (Guid)ctx.Items["UserId"]!;
    return Results.Ok(await walletService.GetTransactionsAsync(userId));
}).RequireAuthorization();

app.Run();








