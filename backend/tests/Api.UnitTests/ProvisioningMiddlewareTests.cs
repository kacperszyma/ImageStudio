using System.Security.Claims;
using Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Users.Contracts;
using Wallet.Contracts;

namespace Api.UnitTests;

public class ProvisioningMiddlewareTests
{
    private const string EmailClaim = "https://imagestudio/email";
    private const string TestSub = "auth0|123456";
    private const string TestEmail = "test@imagestudio.com";
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IWalletService _walletService = Substitute.For<IWalletService>();

    private bool _nextCalled;
    private readonly ProvisioningMiddleware _middleware;

    public ProvisioningMiddlewareTests()
    {
        _middleware = new ProvisioningMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Unauthenticated_request_passes_through_without_provisioning()
    {
        var ctx = new DefaultHttpContext();

        await _middleware.InvokeAsync(ctx, _userService, _walletService);

        _nextCalled.Should().BeTrue();
        await _userService.DidNotReceive().EnsureProvisionedAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Authenticated_request_missing_email_returns_401()
    {
        var ctx = AuthenticatedContext(email: null);

        await _middleware.InvokeAsync(ctx, _userService, _walletService);

        ctx.Response.StatusCode.Should().Be(401);
        _nextCalled.Should().BeFalse();
        await _userService.DidNotReceive().EnsureProvisionedAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task New_user_provisions_user_and_wallet_account()
    {
        var ctx = AuthenticatedContext();
        _userService.EnsureProvisionedAsync(TestSub, TestEmail).Returns((true, TestUserId));

        await _middleware.InvokeAsync(ctx, _userService, _walletService);

        await _userService.Received(1).EnsureProvisionedAsync(TestSub, TestEmail);
        await _walletService.Received(1).EnsureAccountAsync(TestUserId);
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Existing_user_skips_wallet_account_creation()
    {
        var ctx = AuthenticatedContext();
        _userService.EnsureProvisionedAsync(TestSub, TestEmail).Returns((false, TestUserId));

        await _middleware.InvokeAsync(ctx, _userService, _walletService);

        await _userService.Received(1).EnsureProvisionedAsync(TestSub, TestEmail);
        await _walletService.DidNotReceive().EnsureAccountAsync(Arg.Any<Guid>());
        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Authenticated_request_sets_user_context_in_items()
    {
        var ctx = AuthenticatedContext();
        _userService.EnsureProvisionedAsync(TestSub, TestEmail).Returns((false, TestUserId));

        await _middleware.InvokeAsync(ctx, _userService, _walletService);

        ctx.Items["UserId"].Should().Be(TestUserId);
        ctx.Items["UserEmail"].Should().Be(TestEmail);
    }

    private static DefaultHttpContext AuthenticatedContext(string? email = TestEmail)
    {
        var claims = new List<Claim> { new("sub", TestSub) };
        if (email is not null)
            claims.Add(new Claim(EmailClaim, email));

        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }
}
