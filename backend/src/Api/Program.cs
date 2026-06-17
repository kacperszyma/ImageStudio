using Wallet;
using Users;
using Generation;
using Generation.Contracts;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

DotNetEnv.Env.Load("../../../.env");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWalletModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddGenerationModule(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "Frontend",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
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
        });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", opt => opt.WithTitle("API Routes"));
}

app.UseHttpsRedirection();

app.MapGet("/hello", () =>
{
    return Results.Ok("Hello");
}).RequireAuthorization();

app.MapGet("/models", (IGenerationService generationService) =>
{
    return Results.Ok(generationService.GetModels());
});

app.Run();








