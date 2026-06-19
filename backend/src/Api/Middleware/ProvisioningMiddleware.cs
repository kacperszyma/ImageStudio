using Users.Contracts;
using Wallet.Contracts;

namespace Api.Middleware;

public class ProvisioningMiddleware(RequestDelegate next)
{
    private const string EmailClaim = "https://imagestudio/email";

    public async Task InvokeAsync(HttpContext ctx, IUserService userService, IWalletService walletService)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var sub = ctx.User.FindFirst("sub")?.Value;
            var email = ctx.User.FindFirst(EmailClaim)?.Value;

            if (sub is null || email is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var (wasCreated, userId) = await userService.EnsureProvisionedAsync(sub, email);
            if (wasCreated)
                await walletService.EnsureAccountAsync(userId);

            ctx.Items["UserId"] = userId;
            ctx.Items["UserEmail"] = email;
        }

        await next(ctx);
    }
}
