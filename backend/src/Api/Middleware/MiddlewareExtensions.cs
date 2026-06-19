namespace Api.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseProvisioning(this IApplicationBuilder app)
        => app.UseMiddleware<ProvisioningMiddleware>();
}
