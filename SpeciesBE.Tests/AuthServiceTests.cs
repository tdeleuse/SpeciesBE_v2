using Xunit;
using SpeciesBE.Services;

namespace SpeciesBE.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_Definit_Username()
    {
        var js = new FakeJsRuntime();
        var auth = new AuthService(js);

        await auth.LoginAsync("alice");

        Assert.True(auth.IsAuthenticated);
        Assert.Equal("alice", auth.Username);
    }

    [Fact]
    public async Task Logout_Reset_Username()
    {
        var js = new FakeJsRuntime();
        var auth = new AuthService(js);

        await auth.LoginAsync("alice");
        await auth.LogoutAsync();

        Assert.False(auth.IsAuthenticated);
        Assert.Null(auth.Username);
    }
}