using System.Security.Cryptography;
using Wallet;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddWalletModule(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "Frontend",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173");
                      });
});

var app = builder.Build();

app.UseCors("Frontend");

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
});



app.Run();








