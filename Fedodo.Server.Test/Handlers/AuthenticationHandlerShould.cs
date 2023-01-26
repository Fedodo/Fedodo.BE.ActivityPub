using System;
using Fedodo.Server.Handlers;
using Shouldly;
using Xunit;

namespace Fedodo.Server.Test.Handlers;

public class AuthenticationHandlerShould
{
    private readonly AuthenticationHandler _authenticationHandler;

    public AuthenticationHandlerShould()
    {
        _authenticationHandler = new AuthenticationHandler();
    }

    [Theory]
    [InlineData("test", "fuHWfSRTAsrBcqb/seUxJAdubbsKWZs/kWEIG7m0TZiJVWdSiB/PpzHeqtfxzQkUecdnwjkJGj+WAP5rxBSDlw==",
        "h4UzHhoXPbf+qDDCeBR+X9a9Ss4Z/r/ZM1/RNX9n90emzl3NSRDrtIHV2koF6E8CwxblcpKayOKT5TEhOjDy3Amqp/tjppSKVzXnDeWmcFz6/Wqx6GJU9VIpnD8kdLkjBEgLD+2YxMA84u4nY53s0br5CsV7Jn4nT5B969xfgc8=",
        true)]
    [InlineData("", "", "", false)]
    public void VerifyPasswordHash(string password, string passwordHash, string passwordSalt, bool success)
    {
        // Arrange

        // Act
        var result = _authenticationHandler.VerifyPasswordHash(password, Convert.FromBase64String(passwordHash),
            Convert.FromBase64String(passwordSalt));

        // Assert
        result.ShouldBe(success);
    }

    [Theory]
    [InlineData("test")]
    public void CreatePasswordHash(string password)
    {
        // Arrange

        // Act
        _authenticationHandler.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
        var passwordHashString = Convert.ToBase64String(passwordHash);
        var passwordSaltString = Convert.ToBase64String(passwordSalt);

        // Assert
    }
}